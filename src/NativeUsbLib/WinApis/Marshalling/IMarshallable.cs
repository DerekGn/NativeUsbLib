using System;

namespace NativeUsbLib.WinApis.Marshalling
{
    public interface IMarshallable
    {
        int SizeOf { get; }

        void MarshalFrom(IntPtr pointer);
    }
}
