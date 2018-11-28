using NativeUsbLib;
using System;

namespace EnumerateUsb
{
    class Program
    {
        private static int _nestLevel;

        static void Main()
        {
            try
            {
                UsbBus usbBus = new UsbBus();

                foreach (UsbController controller in usbBus.Controller)
                    ShowController(controller);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ShowController(UsbController controller)
        {
            if (controller != null)
            {
                WriteLine($"Controller: [{controller.DeviceDescription}]");

                foreach (UsbHub hub in controller.Hubs)
                {
                    ShowHub(hub);
                }

                Console.WriteLine();
            }
            else
            {
                WriteLine("Controller  = null");
            }
        }

        private static void ShowHub(UsbHub hub)
        {
            IncrementNestLevel();

            if (hub != null)
            {
                WriteLine(hub.IsRootHub
                    ? $"[{hub.DeviceDescription}]"
                    : $"Port[{hub.AdapterNumber}] DeviceConnected: {hub.DeviceDescription}");

                foreach (Device device in hub.ChildDevices)
                {
                    ShowDevice(device);
                }
            }
            else
            {
                WriteLine("Hub = null");
            }

            DecrementNestLevel();
        }

        private static void ShowDevice(Device device)
        {
            if (device != null)
            {
                if (device is UsbHub hub)
                {
                    ShowHub(hub);
                }
                else
                {
                    IncrementNestLevel();

                    string s = "Port[" + device.AdapterNumber + "]";
                    if (device.IsConnected)
                    {
                        s += " DeviceConnected: " + device.DeviceDescription;
                    }
                    else
                    {
                        s += " NoDeviceConnected";
                    }

                    WriteLine(s);

                    DecrementNestLevel();
                }
            }
            else
            {
                WriteLine("Device = null");
            }
        }

        private static void WriteLine(string value)
        {
            Console.WriteLine($"{string.Empty.PadLeft(_nestLevel, '.')}{value}");
        }

        private static void IncrementNestLevel()
        {
            _nestLevel++;
        }

        private static void DecrementNestLevel()
        {
            _nestLevel--;
        }
    }
}
