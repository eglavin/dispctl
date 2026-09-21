using System.Text.Json;
using DispCtl.Lib.Models;

namespace DispCtl.Lib.Functions;

/*
 * Neither Windows' per-device settings nor its display database survive a detach in a form
 * we can restore from: the GDI stored DEVMODE has its position rewritten (and sometimes
 * zeroed) when the topology changes, and the display database is re-persisted with the
 * display switched off. So if 'attach' is to put a display back where it was, dispctl has to
 * record the position itself before giving it up.
 *
 * Entries are keyed on the monitor's device interface path, which is stable across reboots
 * and does not shift when Windows renumbers the \\.\DISPLAYn devices.
 */
public class DisplayLayoutStore
{
	private static readonly string FilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"dispctl",
		"layout.json");

	private static Dictionary<string, DisplayLayout> Load()
	{
		try
		{
			return JsonSerializer.Deserialize<Dictionary<string, DisplayLayout>>(File.ReadAllText(FilePath)) ?? new();
		}
		catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
		{
			return new();
		}
	}

	public static DisplayLayout? Get(string devicePath)
	{
		return Load().GetValueOrDefault(devicePath);
	}

	public static bool TrySave(string devicePath, DisplayLayout layout)
	{
		try
		{
			var layouts = Load();
			layouts[devicePath] = layout;

			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllText(FilePath, JsonSerializer.Serialize(layouts, new JsonSerializerOptions { WriteIndented = true }));

			return true;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}
}
