using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace DispCtl.Lib.Functions;

public class ChangeDisplaySettings
{
	public unsafe static DISP_CHANGE TestDisplaySettings(__char_32 deviceName, DEVMODEW deviceMode)
	{
		return PInvoke.ChangeDisplaySettingsEx(deviceName.ToString(), deviceMode, CDS_TYPE.CDS_TEST, null);
	}

	public unsafe static DISP_CHANGE UpdateDisplaySettings(__char_32 deviceName, DEVMODEW deviceMode)
	{
		return PInvoke.ChangeDisplaySettingsEx(deviceName.ToString(), deviceMode, CDS_TYPE.CDS_UPDATEREGISTRY | CDS_TYPE.CDS_NORESET, null);
	}

	public unsafe static DISP_CHANGE SetPrimaryDisplay(__char_32 deviceName, DEVMODEW deviceMode)
	{
		return PInvoke.ChangeDisplaySettingsEx(deviceName.ToString(), deviceMode, CDS_TYPE.CDS_SET_PRIMARY | CDS_TYPE.CDS_UPDATEREGISTRY | CDS_TYPE.CDS_NORESET, null);
	}

	public unsafe static DISP_CHANGE ApplyDisplaySettings()
	{
		return PInvoke.ChangeDisplaySettingsEx(null, null, 0, null);
	}

	public static string LogDisplayChangeStatus(DISP_CHANGE status)
	{
		switch (status)
		{
			case DISP_CHANGE.DISP_CHANGE_SUCCESSFUL:
				return "Success";

			case DISP_CHANGE.DISP_CHANGE_RESTART:
				return "You'll need to reboot for these changes to apply";

			case DISP_CHANGE.DISP_CHANGE_FAILED:
				return "Failed To Change The Resolution";

			case DISP_CHANGE.DISP_CHANGE_BADMODE:
				return "The graphics mode is not supported";

			case DISP_CHANGE.DISP_CHANGE_NOTUPDATED:
				return "Unable to write settings to the registry";

			case DISP_CHANGE.DISP_CHANGE_BADFLAGS:
				return "An invalid set of flags was passed in";

			case DISP_CHANGE.DISP_CHANGE_BADPARAM:
				return "An invalid parameter was passed in. This can include an invalid flag or combination of flags.";

			case DISP_CHANGE.DISP_CHANGE_BADDUALVIEW:
				return "The settings change was unsuccessful because the system is DualView capable.";

			default:
				return "Failed To Change The Resolution.";
		}
	}
}
