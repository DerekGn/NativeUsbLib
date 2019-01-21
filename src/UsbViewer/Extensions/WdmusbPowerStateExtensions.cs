using System;
using NativeUsbLib.WinApis;

namespace UsbViewer.Extensions
{
    internal static class WdmusbPowerStateExtensions
    {
        public static string Display(this UsbUser.WdmusbPowerState state)
        {
            string result;

            switch (state)
            {
                case UsbUser.WdmusbPowerState.WdmUsbPowerNotMapped:
                    result = "S? (unmapped)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemUnspecified:
                    result = "S? (unspecified)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemWorking:
                    result = "S0 (working)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemSleeping1:
                    result = "S1 (sleep)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemSleeping2:
                    result = "S2 (sleep)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemSleeping3:
                    result = "S3 (sleep)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemHibernate:
                    result = "S4 (Hibernate)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerSystemShutdown:
                    result = "S5 (shutdown)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerDeviceUnspecified:
                    result = "D? (unspecified)";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerDeviceD0:
                    result = "D0";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerDeviceD1:
                    result = "D1";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerDeviceD2:
                    result = "D2";
                    break;
                case UsbUser.WdmusbPowerState.WdmUsbPowerDeviceD3:
                    result = "D3";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            return result;
        }
    }
}
