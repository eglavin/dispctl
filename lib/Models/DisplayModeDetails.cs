using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Models;

public class DeviceModeDetails
{
	public uint Index;
	public uint Width { get => DeviceMode.dmPelsWidth; }
	public uint Height { get => DeviceMode.dmPelsHeight; }
	public uint DisplayFrequency { get => DeviceMode.dmDisplayFrequency; }
	public uint BitsPerPixel { get => DeviceMode.dmBitsPerPel; }
	public string Fields { get => DeviceMode.dmFields.ToString(); }
	public DEVMODEW DeviceMode;

	public DeviceModeDetails(uint index, DEVMODEW deviceMode)
	{
		Index = index;
		DeviceMode = deviceMode;
	}
}