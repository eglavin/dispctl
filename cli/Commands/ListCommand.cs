using DispCtl.Lib.Functions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DispCtl.Cli.Commands;

class ListCommand : Command
{
	protected override int Execute(CommandContext context, CancellationToken cancellationToken)
	{
		var devices = DisplayDevices.GetDisplayDevices();
		AnsiConsole.Write(Formatting.DevicesTable(devices));
		return 0;
	}
}
