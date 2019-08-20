using System;
using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public static class Advapi32
    {
        public enum RegValueKind : ulong
        {
            /// <summary>
            /// No value type
            /// </summary>
            None = (0ul),
            /// <summary>
            /// Unicode nul terminated string
            /// </summary>
            Sz = (1ul),
            /// <summary>
            /// Unicode nul terminated string (with environment variable references)
            /// </summary>
            ExpandSz = (2ul),
            /// <summary>
            /// Free form binary
            /// </summary>
            Binary = (3ul),
            /// <summary>
            /// 32-bit number
            /// </summary>
            Dword = (4ul),
            /// <summary>
            /// 32-bit number (same as DWORD)
            /// </summary>
            DwordLittleEndian = (4ul),
            /// <summary>
            /// 32-bit number
            /// </summary>
            DwordBigEndian = (5ul),
            /// <summary>
            /// Symbolic Link (unicode)
            /// </summary>
            Link = (6ul),
            /// <summary>
            /// Multiple Unicode strings
            /// </summary>
            MultiSz = (7ul),
            /// <summary>
            /// Resource list in the resource map
            /// </summary>
            Resourcelist = (8ul),
            /// <summary>
            /// Resource list in the hardware description
            /// </summary>
            FullResourceDescriptor = (9ul),
            ResourceRequirementsList = (10ul),
            /// <summary>
            /// 64-bit number
            /// </summary>
            Qword = (11ul),
            /// <summary>
            /// 64-bit number (same as QWORD)
            /// </summary>
            QwordLittleEndian = (11ul)
        }

        [DllImport("advapi32", SetLastError = true)]
        public static extern uint RegQueryValueEx(IntPtr hKey, string lpValueName, int lpReserved, ref RegValueKind lpType, IntPtr lpData, ref int lpcbData);
    }
}
