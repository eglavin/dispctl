using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Cli.Commands;

class SetPrimaryCommand : DeviceCommand<DeviceCommandSettings>
{
	protected override int Execute(CommandContext context, DeviceCommandSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		if (!device.DisplayDevice.StateFlags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP))
		{
			AnsiConsole.MarkupLine("[red]Device is not attached to the desktop.[/]");
			return 1;
		}

		var otherDevices = DisplayDevices.GetDisplayDevices()
			.Where(other => other.DisplayDevice.StateFlags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) && other.Index != device.Index)
			.ToList();

		if (otherDevices.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No other attached devices found, primary already set.[/]");
			return 0;
		}

		var currentSettings = DisplayDeviceSettings.GetDeviceDisplaySettings(device.DisplayDevice.DeviceName);
		if (currentSettings is null)
		{
			AnsiConsole.MarkupLine("[red]Unable to read the current display settings for this device.[/]");
			return 1;
		}

		var currentMode = currentSettings.DeviceMode;
		var offsetX = currentMode.Anonymous1.Anonymous2.dmPosition.x;
		var offsetY = currentMode.Anonymous1.Anonymous2.dmPosition.y;
		currentMode.Anonymous1.Anonymous2.dmPosition.x = 0;
		currentMode.Anonymous1.Anonymous2.dmPosition.y = 0;

		var primaryStatus = ChangeDisplaySettings.SetPrimaryDisplay(device.DisplayDevice.DeviceName, currentMode);
		Formatting.WriteStatus($"Set primary ({device.Name})", primaryStatus);

		foreach (var other in otherDevices)
		{
			var otherSettings = DisplayDeviceSettings.GetDeviceDisplaySettings(other.DisplayDevice.DeviceName);
			if (otherSettings is null)
			{
				AnsiConsole.MarkupLine($"[red]Unable to read the current display settings for {other.Name}; skipping reposition.[/]");
				continue;
			}

			var otherMode = otherSettings.DeviceMode;
			otherMode.Anonymous1.Anonymous2.dmPosition.x -= offsetX;
			otherMode.Anonymous1.Anonymous2.dmPosition.y -= offsetY;

			var status = ChangeDisplaySettings.UpdateDisplaySettings(other.DisplayDevice.DeviceName, otherMode);
			Formatting.WriteStatus($"Reposition ({other.Name})", status);
		}

		var applyStatus = ChangeDisplaySettings.ApplyDisplaySettings();
		Formatting.WriteStatus("Apply", applyStatus);

		return 0;
	}
}
