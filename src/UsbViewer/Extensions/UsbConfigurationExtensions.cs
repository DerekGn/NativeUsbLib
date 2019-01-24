using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    internal static class UsbConfigurationExtensions
    {
        public static bool IsSet(this UsbSpec.UsbConfiguration state, UsbSpec.UsbConfiguration compareStatus)
        {
            return (state & compareStatus) == compareStatus;
        }
    }
}
