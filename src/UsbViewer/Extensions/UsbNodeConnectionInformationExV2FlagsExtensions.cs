using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    public static class UsbNodeConnectionInformationExV2FlagsExtensions
    {
        public static bool IsSet(this UsbIoControl.UsbNodeConnectionInformationExV2Flags state, UsbIoControl.UsbNodeConnectionInformationExV2Flags compareStatus)
        {
            return (state & compareStatus) == compareStatus;
        }
    }
}
