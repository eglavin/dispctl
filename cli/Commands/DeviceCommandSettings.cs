using System.ComponentModel;
using Spectre.Console.Cli;

namespace DispCtl.Cli.Commands;

class DeviceCommandSettings : CommandSettings
{
	[CommandArgument(0, "<DEVICE>")]
	[Description("The index of the display device, as shown by the 'list' command.")]
	public uint DeviceIndex { get; set; }
}
