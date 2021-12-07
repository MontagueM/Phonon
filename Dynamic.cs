using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Phonon
{
    class Dynamic
    {
        public uint Hash;
        public string HashString;
        public int MeshCount;
        public bool bHasSkeleton;
        public Dynamic(UInt32 DynamicHash)
        {
            Hash = DynamicHash;
        }

        static string LittleEndian(UInt32 number)
        {
            byte[] bytes = BitConverter.GetBytes(number);
            string retval = "";
            foreach (byte b in bytes)
                retval += b.ToString("X2");
            return retval;
        }

        public bool GetDynamicInfo(string PackagesPath)
        {
            [DllImport(@"C:/Users/monta/OneDrive/Destiny 2 Datamining/CPP/DestinyDataminingCPP/x64/Debug/DestinyDynamicExtractor.dll", 
                EntryPoint = "RequestDynamicInformation", CallingConvention = CallingConvention.StdCall)]
            static extern bool RequestDynamicInformation(string DynamicHash, string pkgsPath, ref int MeshCount, ref bool bHasSkeleton);

            HashString = LittleEndian(Hash);
            // TODO do this on a separate thread as to not lag the UI
            bool status = false;
            while (true)
            {
                try
                {
                    status = RequestDynamicInformation(HashString, PackagesPath, ref MeshCount, ref bHasSkeleton);
                    break;
                }
                catch (System.AccessViolationException e)
                {
                    continue;
                }
            }
            return status & (MeshCount > 0);
        }
    }
}
