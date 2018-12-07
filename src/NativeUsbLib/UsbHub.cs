using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NativeUsbLib.Exceptions;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb hub
    /// </summary>
    public class UsbHub : Device
    {
        #region fields

        /// <summary>
        /// Gets the port count.
        /// </summary>
        /// <value>The port count.</value>
        public int PortCount { get; private set; } = -1;

        /// <summary>
        /// Gets a value indicating whether this instance is bus powered.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is bus powered; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusPowered { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether this instance is root hub.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is root hub; otherwise, <c>false</c>.
        /// </value>
        public bool IsRootHub { get; } = false;

        private UsbApi.UsbNodeInformation m_NodeInformation;
        
        /// <summary>
        /// Gets or sets the node information.
        /// </summary>
        /// <value>The node information.</value>
        public UsbApi.UsbNodeInformation NodeInformation
        {
            get { return m_NodeInformation; }
            protected set
            {
                m_NodeInformation = value;
                IsBusPowered = Convert.ToBoolean(m_NodeInformation.HubInformation.HubIsBusPowered);
                PortCount = m_NodeInformation.HubInformation.HubDescriptor.BNumberOfPorts;
            }
        }

        /// <summary>
        /// Gets or sets the usb hub information.
        /// </summary>
        /// <value>The usb hub information.</value>
        public UsbApi.UsbHubInformationEx HubInformation { get; protected set; }
        
        #endregion

        #region constructor/destructor

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbHub"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbHub(Device parent, UsbApi.UsbDeviceDescriptor deviceDescriptor, string devicePath)
            : base(parent, deviceDescriptor, -1, devicePath)
        {
            DeviceDescription = "Standard-USB-Hub";
            DevicePath = devicePath;

            int nBytesReturned;

            // Open a handle to the host controller.
            IntPtr hostControllerHandle = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);
            if (hostControllerHandle.ToInt64() != UsbApi.InvalidHandleValue)
            {
                UsbApi.UsbRootHubName rootHubName = new UsbApi.UsbRootHubName();
                int nBytes = Marshal.SizeOf(rootHubName);
                IntPtr ptrRootHubName = Marshal.AllocHGlobal(nBytes);

                // Get the root hub name.
                if (KernelApi.DeviceIoControl(hostControllerHandle, UsbApi.IoctlUsbGetRootHubName, ptrRootHubName, nBytes, ptrRootHubName, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    rootHubName = (UsbApi.UsbRootHubName)Marshal.PtrToStructure(ptrRootHubName, typeof(UsbApi.UsbRootHubName));

                    if (rootHubName.ActualLength > 0)
                    {
                        IsRootHub = true;
                        DeviceDescription = "RootHub";
                        DevicePath = @"\\?\" + rootHubName.RootHubName;
                    }
                }

                Marshal.FreeHGlobal(ptrRootHubName);

                // TODO: Get the driver key name for the root hub.

                // Now let's open the hub (based upon the hub name we got above).
                IntPtr hubHandle = KernelApi.CreateFile(DevicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);
                if (hubHandle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    UsbApi.UsbNodeInformation nodeInfo =
                        new UsbApi.UsbNodeInformation { NodeType = UsbApi.UsbHubNode.UsbHub };
                    nBytes = Marshal.SizeOf(nodeInfo);
                    IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                    Marshal.StructureToPtr(nodeInfo, ptrNodeInfo, true);

                    if (KernelApi.DeviceIoControl(hubHandle, UsbApi.IoctlUsbGetNodeInformation, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        NodeInformation = (UsbApi.UsbNodeInformation) Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.UsbNodeInformation));
                    }
                    else
                    {
                        Trace.TraceError($"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbApi.IoctlUsbGetNodeInformation)}] Result: [{KernelApi.GetLastError():X}]");
                    }

                    Marshal.FreeHGlobal(ptrNodeInfo);

                    UsbApi.UsbHubInformationEx hubInfo =
                        new UsbApi.UsbHubInformationEx();
                    nBytes = Marshal.SizeOf(hubInfo);
                    IntPtr ptrHubInfo = Marshal.AllocHGlobal(nBytes);
                    Marshal.StructureToPtr(hubInfo, ptrHubInfo, true);

                    if (KernelApi.DeviceIoControl(hubHandle, UsbApi.IoctlUsbGetNodeInformationEx, ptrHubInfo, nBytes, ptrHubInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        HubInformation = (UsbApi.UsbHubInformationEx)Marshal.PtrToStructure(ptrHubInfo, typeof(UsbApi.UsbHubInformationEx));
                    }
                    else
                    {
                        Trace.TraceError($"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbApi.IoctlUsbGetNodeInformation)}] Result: [{KernelApi.GetLastError():X}]");
                    }

                    Marshal.FreeHGlobal(ptrHubInfo);

                    KernelApi.CloseHandle(hubHandle);
                }

                KernelApi.CloseHandle(hostControllerHandle);

                for (int index = 1; index <= PortCount; index++)
                {

                    // Initialize a new port and save the port.
                    try
                    {
                        Devices.Add(DeviceFactory.BuildDevice(this, index, DevicePath));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Unhandled exception occurred: {0}", e.ToString());
                    }
                }
            }
            else
                throw new UsbHubException("No port found!");
        }

        #endregion

        #endregion
    }
}