using System.Windows.Forms;
using NativeUsbLib;

namespace UsbViewer
{
    /// <summary>
    /// typ of the usb device
    /// </summary>
    public enum DeviceTyp
    {
        /// <summary>
        /// unknown device
        /// </summary>
        Unknown,
        /// <summary>
        /// controller
        /// </summary>
        Controller,
        /// <summary>
        /// root hub
        /// </summary>
        RootHub,
        /// <summary>
        /// hub
        /// </summary>
        Hub,
        /// <summary>
        /// device
        /// </summary>
        Device
    }

    /// <summary>
    /// tree node to show a usb device
    /// </summary>
    public class UsbTreeNode : TreeNode
    {
        /// <summary>
        /// Gets the device.
        /// </summary>
        /// <value>The device.</value>
        public Device Device { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public DeviceTyp Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbTreeNode"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="type">The type.</param>
        /// <param name="contextMenu">The context menu.</param>
        public UsbTreeNode(Device device, DeviceTyp type, ContextMenuStrip contextMenu)
        {
            ContextMenuStrip = contextMenu;
            Device = device;
            Type = type;
        }
    }
}