using System.Runtime.InteropServices;
using DispCtl.Lib.Models;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace DispCtl.Lib.Functions;

/*
 * Windows has no documented, supported API to set per-monitor display scaling (DPI).
 * Reading it is documented (GetDpiForMonitor), but the only way to set it live - without
 * writing PerMonitorSettings registry keys and forcing a display refresh/logoff - is an
 * undocumented pair of DisplayConfigGetDeviceInfo/DisplayConfigSetDeviceInfo request types
 * (-3/-4). These aren't part of the public DISPLAYCONFIG_DEVICE_INFO_TYPE enum, but they're
 * the mechanism Windows' own Settings app uses internally, and have been stable across
 * Windows 10/11 releases. Reverse-engineered and documented at
 * https://github.com/lihas/windows-DPI-scaling-sample.
 */
public class DisplayScaling
{
	// The percentage steps the Windows display settings UI exposes for DPI scaling.
	private static readonly uint[] ScaleSteps = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

	private const int DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE = -3;
	private const int DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE = -4;

	// Mirrors DISPLAYCONFIG_SOURCE_DPI_SCALE_GET. Undocumented, so not part of the CsWin32-generated types;
	// header must stay the first field so a pointer to this struct can double as a DISPLAYCONFIG_DEVICE_INFO_HEADER*.
	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_GET
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
		public int MinScaleRel;
		public int CurScaleRel;
		public int MaxScaleRel;
	}

	// Mirrors DISPLAYCONFIG_SOURCE_DPI_SCALE_SET.
	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_SET
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
		public int ScaleRel;
	}

	private unsafe static (LUID AdapterId, uint SourceId)? FindSource(__char_32 deviceName)
	{
		uint pathCount = 0, modeCount = 0;
		if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) != WIN32_ERROR.ERROR_SUCCESS)
		{
			return null;
		}

		var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
		var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

		if (PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes) != WIN32_ERROR.ERROR_SUCCESS)
		{
			return null;
		}

		foreach (var path in paths)
		{
			var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
			sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
			sourceName.header.size = (uint) Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
			sourceName.header.adapterId = path.sourceInfo.adapterId;
			sourceName.header.id = path.sourceInfo.id;

			if (PInvoke.DisplayConfigGetDeviceInfo(ref sourceName.header) == 0 &&
				sourceName.viewGdiDeviceName.ToString() == deviceName.ToString())
			{
				return (path.sourceInfo.adapterId, path.sourceInfo.id);
			}
		}

		return null;
	}

	public unsafe static DisplayScaleInfo? GetDisplayScaleInfo(__char_32 deviceName)
	{
		var source = FindSource(deviceName);
		if (source is null)
		{
			return null;
		}

		return GetDisplayScaleInfo(source.Value);
	}

	private unsafe static DisplayScaleInfo? GetDisplayScaleInfo((LUID AdapterId, uint SourceId) source)
	{
		var request = new DISPLAYCONFIG_SOURCE_DPI_SCALE_GET();
		request.Header.type = (DISPLAYCONFIG_DEVICE_INFO_TYPE) DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE;
		request.Header.size = (uint) Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_GET>();
		request.Header.adapterId = source.AdapterId;
		request.Header.id = source.SourceId;

		if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*) &request) != 0)
		{
			return null;
		}

		// MinScaleRel/MaxScaleRel come from an undocumented, reverse-engineered request type, so
		// they aren't guaranteed to be well-formed; reject anything that would make the clamp or
		// the array indexing below misbehave instead of throwing.
		if (request.MinScaleRel > request.MaxScaleRel)
		{
			return null;
		}

		var curScaleRel = Math.Clamp(request.CurScaleRel, request.MinScaleRel, request.MaxScaleRel);
		var minAbs = Math.Abs(request.MinScaleRel);

		if (minAbs + request.MaxScaleRel >= ScaleSteps.Length)
		{
			return null;
		}

		return new DisplayScaleInfo
		{
			Minimum = ScaleSteps[0],
			Recommended = ScaleSteps[minAbs],
			Maximum = ScaleSteps[minAbs + request.MaxScaleRel],
			Current = ScaleSteps[minAbs + curScaleRel],
			AvailableScales = ScaleSteps.Take(minAbs + request.MaxScaleRel + 1).ToList()
		};
	}

	public unsafe static bool SetDisplayScale(__char_32 deviceName, uint percent)
	{
		var source = FindSource(deviceName);
		if (source is null)
		{
			return false;
		}

		var info = GetDisplayScaleInfo(source.Value);
		if (info is null)
		{
			return false;
		}

		var targetIndex = Array.IndexOf(ScaleSteps, percent);
		var recommendedIndex = Array.IndexOf(ScaleSteps, info.Recommended);

		if (targetIndex == -1 || recommendedIndex == -1 || !info.AvailableScales.Contains(percent))
		{
			return false;
		}

		var request = new DISPLAYCONFIG_SOURCE_DPI_SCALE_SET();
		request.Header.type = (DISPLAYCONFIG_DEVICE_INFO_TYPE) DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE;
		request.Header.size = (uint) Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_SET>();
		request.Header.adapterId = source.Value.AdapterId;
		request.Header.id = source.Value.SourceId;
		request.ScaleRel = targetIndex - recommendedIndex;

		return PInvoke.DisplayConfigSetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*) &request) == 0;
	}
}
