using System;
using System.Runtime.InteropServices;

namespace AsyncServer
{
    public class StructTransformer
    {
        //structConvertbyte[]
        public static byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr buffer = Marshal.AllocHGlobal(size * 10);
            try
            {
                Marshal.StructureToPtr(structObj, buffer, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        //byte[] Convertsstruct
        public static object BytesToStruct(byte[] bytes, Type strcutType)
        {
            int size = Marshal.SizeOf(strcutType);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return Marshal.PtrToStructure(buffer, strcutType);

            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static int SizeOf(Type strcutType)
        {
            return Marshal.SizeOf(strcutType);
        }
    }
}
