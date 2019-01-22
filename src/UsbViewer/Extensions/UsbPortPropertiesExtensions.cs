using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    public static class UsbPortPropertiesExtensions
    {
        public static bool IsSet(this UsbIoControl.UsbPortProperties state, UsbIoControl.UsbPortProperties compareStatus)
        {
            return (state & compareStatus) == compareStatus;
        }
    }
}
