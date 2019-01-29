using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NativeUsbLib.WinApis
{
    public static class HidApi
    {
        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid(out Guid gHid);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetManufacturerString(SafeFileHandle hidDevice, StringBuilder buffer,
            int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetProductString(IntPtr handle, out IntPtr data, ulong maxBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetSerialNumberString(SafeFileHandle hidDevice, StringBuilder buffer,
            int bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool
            HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HiddAttributes attributes);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_SetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_FlushQueue(SafeFileHandle hidDeviceObject);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetIndexedString(SafeFileHandle hidDeviceObject, int stringIndex,
            byte[] lpString, int bufferLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct HiddAttributes
        {
            public int Size;
            public short VendorId;
            public short ProductId;
            public short VersionNumber;
        }
        
        public const int HidStringLength = 128;
    }
}
