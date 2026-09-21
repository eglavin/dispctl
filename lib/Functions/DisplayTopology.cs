using System.Runtime.InteropServices;
using DispCtl.Lib.Models;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Functions;

/*
 * Attaching or detaching a display through ChangeDisplaySettingsEx (see ChangeDisplaySettings.cs)
 * means hand-computing a non-overlapping DEVMODE for the device being changed and re-asserting
 * every other active device's DEVMODE in the same batch - and even then, on some hardware the
 * driver still drops the other active devices when the change is applied.
 *
 * The CCD (Connecting and Configuring Displays) API's SetDisplayConfig instead takes the whole
 * path list and lets the driver resolve the topology, so we can flip a single path's active bit
 * and leave everything else alone. This is what Windows Settings and Win+P use internally.
 *
 * The tricky part is identifying *which* CCD path corresponds to a GDI device name like
 * "\\.\DISPLAY1", since an inactive path has no GDI source name to match on. The reliable
 * correlation is the monitor's device interface path: EnumDisplayDevices with
 * EDD_GET_DEVICE_INTERFACE_NAME returns the same string as
 * DISPLAYCONFIG_TARGET_DEVICE_NAME.monitorDevicePath for the same physical monitor,
 * whether or not it is currently active.
 */
public class DisplayTopology
{
	private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
	private const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;
	private const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xFFFFFFFF;

	public static string? GetMonitorDevicePath(__char_32 deviceName)
	{
		DISPLAY_DEVICEW monitor = new();
		monitor.cb = (uint) Marshal.SizeOf(monitor);

		if (!PInvoke.EnumDisplayDevices(deviceName.ToString(), 0, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME))
		{
			return null;
		}

		return monitor.DeviceID.ToString();
	}

	private unsafe static string? GetTargetDevicePath(DISPLAYCONFIG_PATH_TARGET_INFO target)
	{
		if (!target.targetAvailable)
		{
			return null;
		}

		var name = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
		name.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
		name.header.size = (uint) Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
		name.header.adapterId = target.adapterId;
		name.header.id = target.id;

		if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*) &name) != 0)
		{
			return null;
		}

		return name.monitorDevicePath.ToString();
	}

	private static bool TryQueryAllPaths(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes)
	{
		paths = Array.Empty<DISPLAYCONFIG_PATH_INFO>();
		modes = Array.Empty<DISPLAYCONFIG_MODE_INFO>();

		if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ALL_PATHS, out var pathCount, out var modeCount) != WIN32_ERROR.ERROR_SUCCESS)
		{
			return false;
		}

		paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
		modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

		return PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ALL_PATHS, ref pathCount, paths, ref modeCount, modes) == WIN32_ERROR.ERROR_SUCCESS;
	}

	private static int FindPath(DISPLAYCONFIG_PATH_INFO[] paths, string devicePath, bool active)
	{
		var usedSourceIds = paths
			.Where(path => (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0)
			.Select(path => path.sourceInfo.id)
			.ToHashSet();

		for (var i = 0; i < paths.Length; i++)
		{
			if (((paths[i].flags & DISPLAYCONFIG_PATH_ACTIVE) != 0) != active)
			{
				continue;
			}

			// A source can only drive one target at a time, so a path we want to light up
			// has to use a source nothing else is already holding.
			if (!active && usedSourceIds.Contains(paths[i].sourceInfo.id))
			{
				continue;
			}

			if (GetTargetDevicePath(paths[i].targetInfo) == devicePath)
			{
				return i;
			}
		}

		return -1;
	}

	private static bool Apply(DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes)
	{
		return PInvoke.SetDisplayConfig(
			paths,
			modes,
			SET_DISPLAY_CONFIG_FLAGS.SDC_APPLY |
			SET_DISPLAY_CONFIG_FLAGS.SDC_USE_SUPPLIED_DISPLAY_CONFIG |
			SET_DISPLAY_CONFIG_FLAGS.SDC_ALLOW_CHANGES |
			SET_DISPLAY_CONFIG_FLAGS.SDC_SAVE_TO_DATABASE) == 0;
	}

	/*
	 * Restoring the display to where it used to sit means supplying the source mode ourselves.
	 * Left to its own devices (SDC_ALLOW_CHANGES with no mode supplied) Windows parks the
	 * display at the right-hand edge of the desktop regardless of where the user had it.
	 */
	public static bool AttachDevice(__char_32 deviceName, DisplayLayout? layout)
	{
		var devicePath = GetMonitorDevicePath(deviceName);
		if (devicePath is null || !TryQueryAllPaths(out var paths, out var modes))
		{
			return false;
		}

		var index = FindPath(paths, devicePath, active: false);
		if (index == -1)
		{
			return false;
		}

		paths[index].flags |= DISPLAYCONFIG_PATH_ACTIVE;
		paths[index].sourceInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
		paths[index].targetInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;

		if (layout is not null)
		{
			Array.Resize(ref modes, modes.Length + 1);
			modes[^1].infoType = DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE;
			modes[^1].id = paths[index].sourceInfo.id;
			modes[^1].adapterId = paths[index].sourceInfo.adapterId;
			modes[^1].Anonymous.sourceMode.width = layout.Width;
			modes[^1].Anonymous.sourceMode.height = layout.Height;
			modes[^1].Anonymous.sourceMode.pixelFormat = DISPLAYCONFIG_PIXELFORMAT.DISPLAYCONFIG_PIXELFORMAT_32BPP;
			modes[^1].Anonymous.sourceMode.position = new POINTL { x = layout.X, y = layout.Y };

			paths[index].sourceInfo.Anonymous.modeInfoIdx = (uint) (modes.Length - 1);
		}

		return Apply(paths, modes);
	}

	/*
	 * Detaching through CCD rather than a zeroed DEVMODE matters for more than symmetry: the
	 * GDI route writes dmPelsWidth/dmPelsHeight of 0 into the device's stored settings, which
	 * destroys the resolution and position that 'attach' needs to put the display back.
	 */
	public static bool DetachDevice(__char_32 deviceName)
	{
		var devicePath = GetMonitorDevicePath(deviceName);
		if (devicePath is null || !TryQueryAllPaths(out var paths, out var modes))
		{
			return false;
		}

		var index = FindPath(paths, devicePath, active: true);
		if (index == -1)
		{
			return false;
		}

		paths[index].flags &= ~DISPLAYCONFIG_PATH_ACTIVE;
		paths[index].sourceInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
		paths[index].targetInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;

		return Apply(paths, modes);
	}
}
