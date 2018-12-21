using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    internal static class UsbHubCapFlagsExtension
    {
        public static bool IsSet(this UsbApi.UsbHubCapFlags state, UsbApi.UsbHubCapFlags compareStatus)
        {
            return (state & compareStatus) == compareStatus;
        }
    }
}
