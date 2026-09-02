namespace DispCtl.Lib.Models;

public class DisplayScaleInfo
{
	public uint Minimum;
	public uint Maximum;
	public uint Recommended;
	public uint Current;
	public List<uint> AvailableScales = new();
}
