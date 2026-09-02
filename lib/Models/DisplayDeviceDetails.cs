using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Models;

public class DisplayDeviceDetails
{
	public uint Index;
	public string Name { get => DisplayDevice.DeviceName.ToString().Split("\\\\.\\").Last(); }
	public int Number { get => Convert.ToInt32(DisplayDevice.DeviceName.ToString().Split("DISPLAY").Last()); }
	public string State { get => DisplayDevice.StateFlags.ToString(); }
	public DISPLAY_DEVICEW DisplayDevice;
	public List<DeviceModeDetails> DisplayModeDetails = new();

	public DisplayDeviceDetails(uint index, DISPLAY_DEVICEW device)
	{
		Index = index;
		DisplayDevice = device;
	}
}
