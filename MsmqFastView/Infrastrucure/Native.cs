using System;
using System.Runtime.InteropServices;

namespace MsmqFastView.Infrastructure
{
    public static class Native
    {
        [DllImport("mqrt.dll", CharSet = CharSet.Unicode)]
        public static extern uint MQMgmtGetInfo(string machineName, string objectName, MQQUEUEPROPS queueProperties);

        [DllImport("mqrt.dll", CharSet = CharSet.Unicode)]
        public static extern void MQFreeMemory(IntPtr pvMemory);

        [StructLayout(LayoutKind.Sequential)]
        public struct MQPROPVARIANT
        {
            public short vt;
            public short wReserved1;
            public short wReserved2;
            public short wReserved3;
            public MQPROPVARIANT_UNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MQPROPVARIANT_UNION
        {
            [FieldOffset(0)]
            public uint ulVal;
            [FieldOffset(0)]
            public CALPWSTR calpwstr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CALPWSTR
        {
            public uint cElems;
            public IntPtr pElems;
        }

        public static class PROPID_MGMT_QUEUE
        {
            public const int SUBQUEUE_COUNT = 26;
            public const int QUEUE_SUBQUEUE_NAMES = 27;
        }

        public static class MQ_ERROR
        {
            public const uint QUEUE_NOT_ACTIVE = 0xC00E0004;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MQQUEUEPROPS
        {
            public int cProp;
            public IntPtr aPropID;
            public IntPtr aPropVar;
            public IntPtr aStatus;
        }
    }
}
