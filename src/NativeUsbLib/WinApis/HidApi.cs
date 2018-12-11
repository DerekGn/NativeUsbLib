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
            Int32 bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool HidD_GetProductString(IntPtr handle, out IntPtr data, ulong maxBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern Boolean HidD_GetSerialNumberString(SafeFileHandle hidDevice, StringBuilder buffer,
            Int32 bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean
            HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HidApi.HiddAttributes attributes);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean HidD_GetFeature(SafeFileHandle hidDeviceObject, Byte[] lpReportBuffer,
            Int32 reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean HidD_SetFeature(SafeFileHandle hidDeviceObject, Byte[] lpReportBuffer,
            Int32 reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean HidD_FlushQueue(SafeFileHandle hidDeviceObject);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean HidD_GetIndexedString(SafeFileHandle hidDeviceObject, Int32 stringIndex,
            Byte[] lpString, Int32 bufferLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct HiddAttributes
        {
            public Int32 Size;
            public Int16 VendorId;
            public Int16 ProductId;
            public Int16 VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HidDescriptor
        {
            public byte BLength;
            public UsbApi.UsbDescriptorType BDescriptorType;
            public short BcdHid;
            public byte BCountry;
            public byte BNumDescriptors;
            public HidApi.HidDescriptorDescList HidDesclist;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HidDescriptorDescList
        {
            public byte BReportType;
            public short WReportLength;
        }

        public const int HidStringLength = 128;
    }
}
