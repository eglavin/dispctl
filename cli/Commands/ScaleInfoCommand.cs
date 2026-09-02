using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DispCtl.Cli.Commands;

class ScaleInfoCommand : DeviceCommand<DeviceCommandSettings>
{
	protected override int Execute(CommandContext context, DeviceCommandSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		var scale = DisplayScaling.GetDisplayScaleInfo(device.DisplayDevice.DeviceName);
		if (scale is null)
		{
			AnsiConsole.MarkupLine("[red]Unable to read display scaling for this device. It may be detached or unsupported.[/]");
			return 1;
		}

		AnsiConsole.Write(Formatting.ScaleTable(scale));
		return 0;
	}
}
