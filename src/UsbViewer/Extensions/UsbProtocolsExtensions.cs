using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    public static class UsbProtocolsExtensions
    {
        public static bool IsSet(this UsbIoControl.UsbProtocols state, UsbIoControl.UsbProtocols compareStatus)
        {
            return (state & compareStatus) == compareStatus;
        }
    }
}
