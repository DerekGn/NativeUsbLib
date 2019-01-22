using System;
using System.Runtime.InteropServices;
using NativeUsbLib.WinApis.Marshalling;
using static NativeUsbLib.WinApis.UsbSpec;

namespace NativeUsbLib.WinApis
{
    public static class UsbIoControl
    {
        public enum UsbProtocols : uint
        {
            Usb110 = 1,
            Usb200 = 2,
            Usb300 = 4
        }

        [Flags]
        public enum UsbNodeConnectionInformationExV2Flags
        {
            DeviceIsOperatingAtSuperSpeedOrHigher = 1,
            DeviceIsSuperSpeedCapableOrHigher = 2,
            DeviceIsOperatingAtSuperSpeedPlusOrHigher = 4,
            DeviceIsSuperSpeedPlusCapableOrHigher = 8
        }

        public enum UsbConnectionStatus
        {
            NoDeviceConnected,
            DeviceConnected,
            DeviceFailedEnumeration,
            DeviceGeneralFailure,
            DeviceCausedOvercurrent,
            DeviceNotEnoughPower,
            DeviceNotEnoughBandwidth,
            DeviceHubNestedTooDeeply,
            DeviceInLegacyHub,
            DeviceEnumerating,
            DeviceReset
        }

        [Flags]
        public enum UsbPortProperties
        {
            PortIsUserConnectable = 1,
            PortIsDebugCapable = 2,
            PortHasMultipleCompanions = 4,
            PortConnectorIsTypeC = 8
        }

        private const int MaxBufferSize = 2048;

        public const int IoctlUsbGetRootHubName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                  (DevIoControl.FileAnyAccess << 14) |
                                                  (UsbIoDefinitions.UsbGetNodeInformation << 2) |
                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeInformation = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                      (DevIoControl.FileAnyAccess << 14) |
                                                      (UsbIoDefinitions.UsbGetNodeInformation << 2) |
                                                      DevIoControl.MethodBuffered;



        public const int IoctlUsbGetHubInformationEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                       (DevIoControl.FileAnyAccess << 14) |
                                                       (UsbIoDefinitions.UsbGetHubInformationEx << 2) |
                                                       DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionInformationEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                  (DevIoControl.FileAnyAccess << 14) |
                                                                  (UsbIoDefinitions.UsbGetNodeConnectionInformationEx <<
                                                                   2) |
                                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionInformationExV2 = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                  (DevIoControl.FileAnyAccess << 14) |
                                                                  (UsbIoDefinitions.UsbGetNodeConnectionInformationExV2 <<
                                                                   2) |
                                                                  DevIoControl.MethodBuffered;


        public const int IoctlUsbGetDescriptorFromNodeConnection = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                   (DevIoControl.FileAnyAccess << 14) |
                                                                   (UsbIoDefinitions
                                                                        .UsbGetDescriptorFromNodeConnection << 2) |
                                                                   DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                         (DevIoControl.FileAnyAccess << 14) |
                                                         (UsbIoDefinitions.UsbGetNodeConnectionName << 2) |
                                                         DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionDriverkeyName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                  (DevIoControl.FileAnyAccess << 14) |
                                                                  (UsbIoDefinitions.UsbGetNodeConnectionDriverkeyName <<
                                                                   2) |
                                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetHubCapabilitiesEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                        (DevIoControl.FileAnyAccess << 14) |
                                                        (UsbIoDefinitions.UsbGetHubCapabilitiesEx << 2) |
                                                        DevIoControl.MethodBuffered;

        public const int IoctlUsbGetPortConnectorProperties = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                              (DevIoControl.FileAnyAccess << 14) |
                                                              (UsbIoDefinitions.UsbGetPortConnectorProperties << 2) |
                                                              DevIoControl.MethodBuffered;

        public struct UsbHubInformationEx : IMarshallable
        {
            public UsbApi.UsbHubType HubType;
            public ushort HighestPortNumber;
            public UsbHubDescriptor UsbHubDescriptor;
            public Usb30HubDescriptor Usb30HubDescriptor;

            public int SizeOf => sizeof(UsbApi.UsbHubType) + sizeof(ushort) +
                                 Math.Max(Marshal.SizeOf(UsbHubDescriptor), Marshal.SizeOf(Usb30HubDescriptor));

            public void MarshalFrom(IntPtr pointer)
            {
                HubType = (UsbApi.UsbHubType) Marshal.ReadInt32(pointer);
                HighestPortNumber = (ushort) Marshal.ReadInt16(pointer, sizeof(UsbApi.UsbHubType));

                var ptr = new IntPtr(pointer.ToInt64() + 6);

                UsbHubDescriptor =
                    (UsbHubDescriptor) Marshal.PtrToStructure(ptr, typeof(UsbHubDescriptor));
                Usb30HubDescriptor =
                    (Usb30HubDescriptor) Marshal.PtrToStructure(ptr, typeof(Usb30HubDescriptor));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbNodeConnectionInformationEx
        {
            public uint ConnectionIndex;
            public UsbDeviceDescriptor DeviceDescriptor;
            public byte CurrentConfigurationValue;
            public UsbDeviceSpeed Speed;
            public byte DeviceIsHub;
            public short DeviceAddress;
            public int NumberOfOpenPipes;
            public UsbConnectionStatus ConnectionStatus;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Usb30HubDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public byte NumberOfPorts;
            public ushort HubCharacteristics;
            public byte PowerOnToPowerGood;
            public byte HubControlCurrent;
            public byte HubHdrDecLat;
            public ushort HubDelay;
            public ushort DeviceRemovable;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbNodeConnectionName
        {
            public int ConnectionIndex;
            public int ActualLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string NodeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbPortConnectorProperties
        {
            public uint ConnectionIndex;
            public uint ActualLength;
            public UsbPortProperties Properties;
            public ushort CompanionIndex;
            public ushort CompanionPortNumber;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string CompanionHubSymbolicLinkName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbNodeConnectionInformationExV2
        {
            // one based port number
            public uint ConnectionIndex;

            // length of the structure
            public uint Length;

            // On input a bitmask that indicates which USB protocols are understood by the caller
            // On output a bitmask that indicates which USB signaling protocols are supported by the port
            public UsbProtocols SupportedUsbProtocols;

            // A bitmask indicating properties of the connected device or port
            public UsbNodeConnectionInformationExV2Flags Flags;
        }
    }
}