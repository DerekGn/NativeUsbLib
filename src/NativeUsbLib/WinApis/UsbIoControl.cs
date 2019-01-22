using System;
using System.Runtime.InteropServices;
using NativeUsbLib.WinApis.Marshalling;
using static NativeUsbLib.WinApis.UsbSpec;

namespace NativeUsbLib.WinApis
{
    public static class UsbIoControl
    {
        [Flags]
        public enum UsbPortProperties
        {
            PortIsUserConnectable = 1,
            PortIsDebugCapable = 2,
            PortHasMultipleCompanions = 4,
            PortConnectorIsTypeC = 8
        }

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

        public struct UsbPortConnectorProperties
        {
            // one based port number
            public int ConnectionIndex;

            // The number of bytes required to hold the entire USB_PORT_CONNECTOR_PROPERTIES
            // structure, including the full CompanionHubSymbolicLinkName string
            public int ActualLength;

            // bitmask of flags indicating properties and capabilities of the port
            public UsbPortProperties UsbPortProperties;

            // Zero based index number of the companion port being queried.
            public ushort CompanionIndex;

            // Port number of the companion port
            public ushort CompanionPortNumber;

            // Symbolic link name for the companion hub
            public char CompanionHubSymbolicLinkName;
        }

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
            public int ConnectionIndex;
            public UsbSpec.UsbDeviceDescriptor DeviceDescriptor;
            public byte CurrentConfigurationValue;
            public UsbSpec.UsbDeviceSpeed Speed;
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
    }
}