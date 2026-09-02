using DispCtl.Lib.Models;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Functions;

public class DisplayDevices
{
	public static List<DisplayDeviceDetails> GetDisplayDevices()
	{
		List<DisplayDeviceDetails> displayDevices = new();
		try
		{
			DISPLAY_DEVICEW device = new();
			device.cb = (uint) Marshal.SizeOf(device);

			for (uint index = 0; PInvoke.EnumDisplayDevices(null, index, ref device, 0); index++)
			{
				displayDevices.Add(new DisplayDeviceDetails(index, device));
				device.cb = (uint) Marshal.SizeOf(device);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}

		return displayDevices;
	}
}
