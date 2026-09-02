using System.ComponentModel;
using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DispCtl.Cli.Commands;

class SetScaleSettings : DeviceCommandSettings
{
	[CommandArgument(1, "<PERCENT>")]
	[Description("The scale percentage to apply, as shown by the 'scale-info' command (e.g. 150).")]
	public uint Percent { get; set; }
}

class SetScaleCommand : DeviceCommand<SetScaleSettings>
{
	protected override int Execute(CommandContext context, SetScaleSettings settings, CancellationToken cancellationToken)
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

		if (!scale.AvailableScales.Contains(settings.Percent))
		{
			var available = string.Join(", ", scale.AvailableScales.Select(s => $"{s}%"));
			AnsiConsole.MarkupLineInterpolated($"[red]{settings.Percent}% is not a supported scale for this device. Available: {available}[/]");
			return 1;
		}

		var success = DisplayScaling.SetDisplayScale(device.DisplayDevice.DeviceName, settings.Percent);
		Formatting.WriteResult($"Set scale to {settings.Percent}%", success);

		return success ? 0 : 1;
	}
}
