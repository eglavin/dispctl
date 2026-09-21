using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Cli.Commands;

class AttachCommand : DeviceCommand<DeviceCommandSettings>
{
	protected override int Execute(CommandContext context, DeviceCommandSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		if (device.DisplayDevice.StateFlags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP))
		{
			AnsiConsole.MarkupLine("[red]Device is already attached.[/]");
			return 1;
		}

		// Restore the position this display had when it was detached. With nothing on record
		// Windows picks the layout itself, which parks it at the edge of the desktop.
		var devicePath = DisplayTopology.GetMonitorDevicePath(device.DisplayDevice.DeviceName);
		var layout = devicePath is null ? null : DisplayLayoutStore.Get(devicePath);

		if (!DisplayTopology.AttachDevice(device.DisplayDevice.DeviceName, layout))
		{
			AnsiConsole.MarkupLine("[red]Unable to attach this device; it may not have a monitor connected.[/]");
			return 1;
		}

		var updatedDevice = FindDevice(settings.DeviceIndex);
		if (updatedDevice is null || !updatedDevice.DisplayDevice.StateFlags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP))
		{
			AnsiConsole.MarkupLine("[red]Windows did not attach this device; it may not have a monitor connected.[/]");
			return 1;
		}

		AnsiConsole.MarkupLineInterpolated($"[green]{updatedDevice.Name} attached.[/]");
		return 0;
	}
}
