using System.ComponentModel;
using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Cli.Commands;

class SetResolutionSettings : DeviceCommandSettings
{
	[CommandArgument(1, "<MODE>")]
	[Description("The index of the display mode, as shown by the 'modes' command.")]
	public uint ModeIndex { get; set; }
}

class SetResolutionCommand : DeviceCommand<SetResolutionSettings>
{
	protected override int Execute(CommandContext context, SetResolutionSettings settings, CancellationToken cancellationToken)
	{
		var device = FindDevice(settings.DeviceIndex);
		if (device is null)
		{
			return 1;
		}

		var selectedMode = DisplayDeviceSettings.GetAllDisplayDeviceSettings(device.DisplayDevice.DeviceName)
			.Find(mode => mode.Index == settings.ModeIndex);

		if (selectedMode is null)
		{
			AnsiConsole.MarkupLineInterpolated($"[red]No display mode found with index {settings.ModeIndex}. Run 'modes {settings.DeviceIndex}' to see available modes.[/]");
			return 1;
		}

		var newMode = DisplayDeviceSettings.GetDeviceDisplaySettings(device.DisplayDevice.DeviceName);
		if (newMode is null)
		{
			AnsiConsole.MarkupLine("[red]Unable to read the current display settings for this device.[/]");
			return 1;
		}

		newMode.DeviceMode.dmPelsWidth = selectedMode.DeviceMode.dmPelsWidth;
		newMode.DeviceMode.dmPelsHeight = selectedMode.DeviceMode.dmPelsHeight;

		var testStatus = ChangeDisplaySettings.TestDisplaySettings(device.DisplayDevice.DeviceName, newMode.DeviceMode);
		Formatting.WriteStatus("Test", testStatus);
		if (testStatus != DISP_CHANGE.DISP_CHANGE_SUCCESSFUL)
		{
			return 1;
		}

		var changeStatus = ChangeDisplaySettings.UpdateDisplaySettings(device.DisplayDevice.DeviceName, newMode.DeviceMode);
		Formatting.WriteStatus("Change", changeStatus);

		var applyStatus = ChangeDisplaySettings.ApplyDisplaySettings();
		Formatting.WriteStatus("Apply", applyStatus);

		return 0;
	}
}
