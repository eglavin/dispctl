using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DispCtl.Cli.Commands;

class ModesCommand : DeviceCommand<DeviceCommandSettings>
{
	protected override int Execute(CommandContext context, DeviceCommandSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		var modes = DisplayDeviceSettings.GetAllDisplayDeviceSettings(device.DisplayDevice.DeviceName);
		AnsiConsole.Write(Formatting.ModesTable(modes));
		return 0;
	}
}
