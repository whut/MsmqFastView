using System;
using System.Messaging;
using System.Runtime.InteropServices;

namespace MsmqFastView.Infrastructure
{
    public static class MessageQueueExtensions
    {
        public static int GetNumberOfSubqueues(this MessageQueue messageQueue)
        {
            return GetNumberOfSubqueues(messageQueue.FormatName);
        }

        public static string[] GetSubqueueNames(this MessageQueue messageQueue)
        {
            return GetSubqueueNames(messageQueue.FormatName);
        }

        public static int GetNumberOfMessages(this MessageQueue messageQueue)
        {
            return GetNumberOfMessages(messageQueue.FormatName);
        }

        public static int GetNumberOfSubqueues(string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                NativeMethods.PROPID_MGMT_QUEUE.SUBQUEUE_COUNT, 
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            NativeMethods.MQPROPVARIANT[] propertyValues = new NativeMethods.MQPROPVARIANT[1]
            {
                new NativeMethods.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            NativeMethods.MQQUEUEPROPS queueProperties = new NativeMethods.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = NativeMethods.MQMgmtGetInfo(Environment.MachineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == NativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE)
            {
                return 0;
            }

            MsmqException.Assert(returnCode == 0, string.Format("MQMgmtGetInfo returned error: {0:x8}", returnCode));
            MsmqException.Assert(((VarEnum)propertyValues[0].vt) == VarEnum.VT_UI4, "Unexpected type returned, should be " + VarEnum.VT_UI4 + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            return (int)propertyValues[0].union.ulVal;
        }

        public static string[] GetSubqueueNames(string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                NativeMethods.PROPID_MGMT_QUEUE.QUEUE_SUBQUEUE_NAMES
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            NativeMethods.MQPROPVARIANT[] propertyValues = new NativeMethods.MQPROPVARIANT[1]
            {
                new NativeMethods.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            NativeMethods.MQQUEUEPROPS queueProperties = new NativeMethods.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = NativeMethods.MQMgmtGetInfo(Environment.MachineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == NativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE)
            {
                return new string[0];
            }

            MsmqException.Assert(returnCode == 0, string.Format("MQMgmtGetInfo returned error: {0:x8}", returnCode));
            MsmqException.Assert(propertyValues[0].vt == (short)(VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR), "Unexpected type returned, should be " + (VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR) + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            IntPtr[] elems = new IntPtr[propertyValues[0].union.calpwstr.cElems];
            Marshal.Copy(propertyValues[0].union.calpwstr.pElems, elems, 0, (int)propertyValues[0].union.calpwstr.cElems);

            string[] subQueueNames = new string[elems.Length];
            for (int i = 0; i < elems.Length; i++)
            {
                subQueueNames[i] = Marshal.PtrToStringUni(elems[i]);
                NativeMethods.MQFreeMemory(elems[i]);
            }

            NativeMethods.MQFreeMemory(propertyValues[0].union.calpwstr.pElems);

            return subQueueNames;
        }

        public static int GetNumberOfMessages(string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                NativeMethods.PROPID_MGMT_QUEUE.MESSAGE_COUNT, 
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            NativeMethods.MQPROPVARIANT[] propertyValues = new NativeMethods.MQPROPVARIANT[1]
            {
                new NativeMethods.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            NativeMethods.MQQUEUEPROPS queueProperties = new NativeMethods.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = NativeMethods.MQMgmtGetInfo(Environment.MachineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == NativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE)
            {
                return 0;
            }

            MsmqException.Assert(returnCode == 0, string.Format("MQMgmtGetInfo returned error: {0:x8}", returnCode));
            MsmqException.Assert(((VarEnum)propertyValues[0].vt) == VarEnum.VT_UI4, "Unexpected type returned, should be " + VarEnum.VT_UI4 + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            return (int)propertyValues[0].union.ulVal;
        }
    }
}
