using DispCtl.Lib.Functions;
using DispCtl.Lib.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Cli.Commands;

class DetachCommand : DeviceCommand<DeviceCommandSettings>
{
	protected override int Execute(CommandContext context, DeviceCommandSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		var flags = device.DisplayDevice.StateFlags;
		if (!flags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP))
		{
			AnsiConsole.MarkupLine("[red]Device is already detached.[/]");
			return 1;
		}

		if (flags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_PRIMARY_DEVICE))
		{
			AnsiConsole.MarkupLine("[red]Cannot detach the primary device. Set another device as primary first.[/]");
			return 1;
		}

		// Windows does not keep the position of a detached display anywhere we can read it
		// back, so record it now for 'attach' to restore.
		var current = DisplayDeviceSettings.GetDeviceDisplaySettings(device.DisplayDevice.DeviceName);
		var devicePath = DisplayTopology.GetMonitorDevicePath(device.DisplayDevice.DeviceName);

		if (!DisplayTopology.DetachDevice(device.DisplayDevice.DeviceName))
		{
			AnsiConsole.MarkupLine("[red]Unable to detach this device.[/]");
			return 1;
		}

		if (current is not null && devicePath is not null)
		{
			var position = current.DeviceMode.Anonymous1.Anonymous2.dmPosition;
			var layout = new DisplayLayout(current.DeviceMode.dmPelsWidth, current.DeviceMode.dmPelsHeight, position.x, position.y);

			if (!DisplayLayoutStore.TrySave(devicePath, layout))
			{
				AnsiConsole.MarkupLine("[yellow]Could not record this display's position; 'attach' will let Windows choose one.[/]");
			}
		}

		AnsiConsole.MarkupLineInterpolated($"[green]{device.Name} detached.[/]");
		return 0;
	}
}
