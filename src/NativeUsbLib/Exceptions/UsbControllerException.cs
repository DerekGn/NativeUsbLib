using System;
using System.Runtime.Serialization;

namespace NativeUsbLib.Exceptions
{
    [Serializable]
    public class UsbControllerException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public UsbControllerException()
        {
        }

        public UsbControllerException(string message) : base(message)
        {
        }

        public UsbControllerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UsbControllerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}