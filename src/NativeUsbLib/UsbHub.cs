#region references

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

#endregion

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

        private UsbApi.USB_NODE_INFORMATION m_NodeInformation;
        /// <summary>
        /// Gets or sets the node information.
        /// </summary>
        /// <value>The node information.</value>
        public UsbApi.USB_NODE_INFORMATION NodeInformation
        {
            get { return m_NodeInformation; }
            set
            {
                m_NodeInformation = value;
                IsBusPowered = Convert.ToBoolean(m_NodeInformation.HubInformation.HubIsBusPowered);
                PortCount = m_NodeInformation.HubInformation.HubDescriptor.bNumberOfPorts;
            }
        }

        #endregion

        #region constructor/destructor

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbHub"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbHub(Device parent, UsbApi.USB_DEVICE_DESCRIPTOR deviceDescriptor, string devicePath)
            : base(parent, deviceDescriptor, -1, devicePath)
        {
            DeviceDescription = "Standard-USB-Hub";
            DevicePath = devicePath;

            int nBytesReturned;

            // Open a handle to the host controller.
            IntPtr handel1 = UsbApi.CreateFile(devicePath, UsbApi.GENERIC_WRITE, UsbApi.FILE_SHARE_WRITE, IntPtr.Zero, UsbApi.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handel1.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {

                UsbApi.USB_ROOT_HUB_NAME rootHubName = new UsbApi.USB_ROOT_HUB_NAME();
                int nBytes = Marshal.SizeOf(rootHubName);
                IntPtr ptrRootHubName = Marshal.AllocHGlobal(nBytes);

                // Get the root hub name.
                if (UsbApi.DeviceIoControl(handel1, UsbApi.IOCTL_USB_GET_ROOT_HUB_NAME, ptrRootHubName, nBytes, ptrRootHubName, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    rootHubName = (UsbApi.USB_ROOT_HUB_NAME)Marshal.PtrToStructure(ptrRootHubName, typeof(UsbApi.USB_ROOT_HUB_NAME));

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
                IntPtr handel2 = UsbApi.CreateFile(DevicePath, UsbApi.GENERIC_WRITE, UsbApi.FILE_SHARE_WRITE, IntPtr.Zero, UsbApi.OPEN_EXISTING, 0, IntPtr.Zero);
                if (handel2.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
                {

                    UsbApi.USB_NODE_INFORMATION nodeInfo =
                        new UsbApi.USB_NODE_INFORMATION {NodeType = UsbApi.USB_HUB_NODE.UsbHub};
                    nBytes = Marshal.SizeOf(nodeInfo);
                    IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                    Marshal.StructureToPtr(nodeInfo, ptrNodeInfo, true);

                    // Get the hub information.
                    if (UsbApi.DeviceIoControl(handel2, UsbApi.IOCTL_USB_GET_NODE_INFORMATION, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        NodeInformation = (UsbApi.USB_NODE_INFORMATION)Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.USB_NODE_INFORMATION));
                    }

                    Marshal.FreeHGlobal(ptrNodeInfo);
                    UsbApi.CloseHandle(handel2);
                }

                UsbApi.CloseHandle(handel1);

                for (int index = 1; index <= PortCount; index++)
                {

                    // Initialize a new port and save the port.
                    try
                    {
                        //this.childs.Add(new UsbPort(this, index, this.DevicePath));
                        Devices.Add(DeviceFactory.BuildDevice(this, index, DevicePath));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Unhandled exception occurred: {0}", e.ToString());
                    }
                }
            }
            else
                throw new Exception("No port found!");
        }

        #endregion

        #endregion
    }
}