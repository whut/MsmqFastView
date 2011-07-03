using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using MsmqFastView.Infrastructure;

namespace MsmqFastView.Infrastrucure
{
    public static class NativeWrapper
    {
        public static int GetNumberOfSubqueues(string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                Native.PROPID_MGMT_QUEUE.SUBQUEUE_COUNT, 
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            Native.MQPROPVARIANT[] propertyValues = new Native.MQPROPVARIANT[1]
            {
                new Native.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            Native.MQQUEUEPROPS queueProperties = new Native.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = Native.MQMgmtGetInfo(Environment.MachineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == Native.MQ_ERROR.QUEUE_NOT_ACTIVE)
            {
                return 0;
            }

            Debug.Assert(returnCode == 0, string.Format("MQMgmtGetInfo returned error: {0:x8}", returnCode));
            Debug.Assert(((VarEnum)propertyValues[0].vt) == VarEnum.VT_UI4, "Unexpected type returned, should be " + VarEnum.VT_UI4 + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            return (int)propertyValues[0].union.ulVal;
        }

        public static IEnumerable<string> GetSubqueueNames(string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                Native.PROPID_MGMT_QUEUE.QUEUE_SUBQUEUE_NAMES
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            Native.MQPROPVARIANT[] propertyValues = new Native.MQPROPVARIANT[1]
            {
                new Native.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            Native.MQQUEUEPROPS queueProperties = new Native.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = Native.MQMgmtGetInfo(Environment.MachineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == Native.MQ_ERROR.QUEUE_NOT_ACTIVE)
            {
                return Enumerable.Empty<string>();
            }

            Debug.Assert(returnCode == 0, string.Format("MQMgmtGetInfo returned error: {0:x8}", returnCode));
            Debug.Assert(propertyValues[0].vt == (short)(VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR), "Unexpected type returned, should be " + (VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR) + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            IntPtr[] elems = new IntPtr[propertyValues[0].union.calpwstr.cElems];
            Marshal.Copy(propertyValues[0].union.calpwstr.pElems, elems, 0, (int)propertyValues[0].union.calpwstr.cElems);

            List<string> subQueueNames = new List<string>();
            foreach (var elem in elems)
            {
                subQueueNames.Add(Marshal.PtrToStringUni(elem));
                Native.MQFreeMemory(elem);
            }

            Native.MQFreeMemory(propertyValues[0].union.calpwstr.pElems);

            return subQueueNames;
        }
    }
}
