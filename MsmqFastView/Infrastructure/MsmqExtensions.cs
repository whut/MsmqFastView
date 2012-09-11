﻿using System;
using System.Globalization;
using System.Linq;
using System.Messaging;
using System.Runtime.InteropServices;

namespace MsmqFastView.Infrastructure
{
    public static class MsmqExtensions
    {
        public static int GetNumberOfSubqueues(this MessageQueue messageQueue)
        {
            return GetNumberOfSubqueues(messageQueue.MachineName, messageQueue.FormatName);
        }

        public static string[] GetSubqueueNames(this MessageQueue messageQueue)
        {
            return GetSubqueueNames(messageQueue.MachineName, messageQueue.FormatName);
        }

        /// <summary>
        /// Returns numer of messages in given queue (including messages in subqueues).
        /// When given subqueue throws exception.
        /// </summary>
        /// <param name="messageQueue">The queue.</param>
        /// <returns>Number of messages.</returns>
        public static int GetNumberOfMessages(this MessageQueue messageQueue)
        {
            return GetNumberOfMessages(messageQueue.MachineName, messageQueue.FormatName);
        }

        public static int GetNumberOfMessages(string machineName, string queueFormatName)
        {
            return GetIntProperty(
                machineName,
                queueFormatName,
                MsmqNativeMethods.PROPID_MGMT_QUEUE.MESSAGE_COUNT,
                new[] { MsmqNativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE });
        }

        public static int GetNumberOfMessagesInJournal(string machineName, string queueFormatName)
        {
            return GetIntProperty(
                machineName,
                queueFormatName,
                MsmqNativeMethods.PROPID_MGMT_QUEUE.JOURNAL_MESSAGE_COUNT,
                new[] { MsmqNativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE });
        }

        public static int GetNumberOfSubqueues(string machineName, string queueFormatName)
        {
            return GetIntProperty(
                machineName,
                queueFormatName,
                MsmqNativeMethods.PROPID_MGMT_QUEUE.SUBQUEUE_COUNT,
                new[] { MsmqNativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE, MsmqNativeMethods.MQ_ERROR.ILLEGAL_PROPID });
        }

        public static string[] GetSubqueueNames(string machineName, string queueFormatName)
        {
            int[] propertyIds = new int[1] 
            {
                MsmqNativeMethods.PROPID_MGMT_QUEUE.QUEUE_SUBQUEUE_NAMES
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            MsmqNativeMethods.MQPROPVARIANT[] propertyValues = new MsmqNativeMethods.MQPROPVARIANT[1]
            {
                new MsmqNativeMethods.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            MsmqNativeMethods.MQQUEUEPROPS queueProperties = new MsmqNativeMethods.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = MsmqNativeMethods.MQMgmtGetInfo(machineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (returnCode == MsmqNativeMethods.MQ_ERROR.QUEUE_NOT_ACTIVE
                || returnCode == MsmqNativeMethods.MQ_ERROR.ILLEGAL_PROPID)
            {
                return new string[0];
            }

            MsmqException.Assert(returnCode == 0, string.Format(CultureInfo.InvariantCulture, "MQMgmtGetInfo returned error: {0:x8}", returnCode));
            MsmqException.Assert(propertyValues[0].vt == (short)(VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR), "Unexpected type returned, should be " + (VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR) + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            IntPtr[] elems = new IntPtr[propertyValues[0].union.calpwstr.cElems];
            Marshal.Copy(propertyValues[0].union.calpwstr.pElems, elems, 0, (int)propertyValues[0].union.calpwstr.cElems);

            string[] subQueueNames = new string[elems.Length];
            for (int i = 0; i < elems.Length; i++)
            {
                subQueueNames[i] = Marshal.PtrToStringUni(elems[i]);
                MsmqNativeMethods.MQFreeMemory(elems[i]);
            }

            MsmqNativeMethods.MQFreeMemory(propertyValues[0].union.calpwstr.pElems);

            return subQueueNames;
        }

        private static int GetIntProperty(string machineName, string queueFormatName, int propId, uint[] allowedReturnCodes)
        {
            int[] propertyIds = new int[1] 
            {
                propId, 
            };
            GCHandle aPropId = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);

            MsmqNativeMethods.MQPROPVARIANT[] propertyValues = new MsmqNativeMethods.MQPROPVARIANT[1]
            {
                new MsmqNativeMethods.MQPROPVARIANT() { vt = (short)VarEnum.VT_NULL }
            };
            GCHandle aPropVar = GCHandle.Alloc(propertyValues, GCHandleType.Pinned);

            MsmqNativeMethods.MQQUEUEPROPS queueProperties = new MsmqNativeMethods.MQQUEUEPROPS()
            {
                cProp = 1,
                aPropID = aPropId.AddrOfPinnedObject(),
                aPropVar = aPropVar.AddrOfPinnedObject(),
                aStatus = IntPtr.Zero
            };

            uint returnCode = MsmqNativeMethods.MQMgmtGetInfo(machineName, "QUEUE=" + queueFormatName, queueProperties);

            aPropId.Free();
            aPropVar.Free();

            if (allowedReturnCodes != null && allowedReturnCodes.Contains(returnCode))
            {
                return 0;
            }

            MsmqException.Assert(returnCode == 0, string.Format(CultureInfo.InvariantCulture, "MQMgmtGetInfo returned error: {0:x8}", returnCode));
            MsmqException.Assert(((VarEnum)propertyValues[0].vt) == VarEnum.VT_UI4, "Unexpected type returned, should be " + VarEnum.VT_UI4 + ", but was " + ((VarEnum)propertyValues[0].vt) + ".");

            return (int)propertyValues[0].union.ulVal;
        }
    }
}
