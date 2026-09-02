using DispCtl.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
	config.SetApplicationName("dispctl");

	config.AddCommand<ListCommand>("list")
		.WithDescription("List all display devices and their current resolution.");

	config.AddCommand<ModesCommand>("modes")
		.WithDescription("List the available display modes for a device.")
		.WithExample("modes", "0");

	config.AddCommand<SetResolutionCommand>("set-resolution")
		.WithDescription("Apply a display mode (from 'modes') to a device.")
		.WithExample("set-resolution", "0", "3");

	config.AddCommand<SetPrimaryCommand>("set-primary")
		.WithDescription("Set a device as the primary monitor.")
		.WithExample("set-primary", "1");

	config.AddCommand<ScaleInfoCommand>("scale-info")
		.WithDescription("Show the display scaling (DPI) options for a device.")
		.WithExample("scale-info", "0");

	config.AddCommand<SetScaleCommand>("set-scale")
		.WithDescription("Set the display scaling (DPI) percentage for a device.")
		.WithExample("set-scale", "0", "150");

	config.AddCommand<AttachCommand>("attach")
		.WithDescription("Attach a detached device to the desktop.")
		.WithExample("attach", "2");

	config.AddCommand<DetachCommand>("detach")
		.WithDescription("Detach a device from the desktop.")
		.WithExample("detach", "2");
});

return app.Run(args);
