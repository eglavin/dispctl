using System.Runtime.InteropServices;
using DispCtl.Lib.Models;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Functions;

public class DisplayDeviceSettings
{
	private static uint MIN_WIDTH = 800;
	private static uint MIN_HEIGHT = 600;

	public unsafe static DeviceModeDetails? GetDeviceDisplaySettings(__char_32 deviceName, ENUM_DISPLAY_SETTINGS_MODE mode = ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS)
	{
		DEVMODEW deviceMode = new();
		if (!PInvoke.EnumDisplaySettings(deviceName.ToString(), mode, ref deviceMode))
		{
			return null;
		}
		return new DeviceModeDetails((uint) mode, deviceMode);
	}

	public unsafe static List<DeviceModeDetails> GetAllDisplayDeviceSettings(__char_32 deviceName, bool filtered = true)
	{
		List<DeviceModeDetails> displayModeDetails = new();
		try
		{
			DEVMODEW deviceMode = new();

			for (uint index = 0; PInvoke.EnumDisplaySettings(deviceName.ToString(), (ENUM_DISPLAY_SETTINGS_MODE)index, ref deviceMode); index++)
			{
				if (filtered &&
					deviceMode.dmPelsWidth >= MIN_WIDTH &&
					deviceMode.dmPelsHeight >= MIN_HEIGHT &&
					displayModeDetails.FindIndex((d) => d.Width == deviceMode.dmPelsWidth &&
														d.Height == deviceMode.dmPelsHeight) == -1 ||
					!filtered)
				{
					displayModeDetails.Add(new DeviceModeDetails(index, deviceMode));
				}
			}

			// Sort resolutions by width, height and index
			displayModeDetails.Sort((a, z) =>
			{
				var dif = a.Width.CompareTo(z.Width);
				if (dif == 0) dif = a.Height.CompareTo(z.Height);
				if (dif == 0) dif = a.Index.CompareTo(z.Index);
				return dif;
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}

		return displayModeDetails;
	}
}
