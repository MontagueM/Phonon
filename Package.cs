using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Phonon
{
    public static class StructConverter
    {
        public static T ToStructure<T>(this byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)); }
            finally { handle.Free(); }
        }

        public static T ToClass<T>(this byte[] bytes) where T : class
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)); }
            finally { handle.Free(); }
        }
    }

    
    struct PkgHeader
    {
        public ushort PkgID;
        public ushort PatchID;
        public uint EntryTableOffset;
        public uint EntryTableCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PkgEntry
    {
        public uint Reference;
        public uint EntryB;
        public uint EntryC;
        public uint EntryD;
    }

    class Package
    {
        public string Path;
        public string Name;  // The name excludes the patch ID
        private BinaryReader Handle;
        public PkgHeader Header;
        private List<int> DynamicHashIndices;
        public List<Dynamic> Dynamics;

        public Package()
        {

        }
        public Package(string PkgPath)
        {
            Path = PkgPath;
            MakeName();
            GetHandle();
            ReadHeader();
            CloseHandle();
        }

        public string MakeName()
        {
            Name = Path.Split("\\").Last();
            Name = Name.Substring(4, Name.Length - 10);
            return Name;
        }

        public void GetDynamics(string PackagesPath)
        {
            Dynamics = new List<Dynamic>();
            foreach(int DynamicHashIndex in DynamicHashIndices)
            {
                Dynamic dynamic = new Dynamic((UInt32)(0x80800000 + DynamicHashIndex + Header.PkgID * 8192));
                bool success = dynamic.GetDynamicInfo(PackagesPath);
                if (success)
                {
                    Dynamics.Add(dynamic);
                }
            }
        }

        public bool GetHandle()
        {
            Handle = new BinaryReader(File.Open(Path, FileMode.Open));
            return true;
        }

        public bool CloseHandle()
        {
            if (Handle != null)
            {
                Handle.Close();
                return true;
            }
            return false;
        }

        public void ReadHeader()
        {
            if (Header.PkgID > 0)
            {
                return;
            }
            Header = new PkgHeader();
            Handle.BaseStream.Seek(0x10, SeekOrigin.Begin);
            Header.PkgID = Handle.ReadUInt16();
            Handle.BaseStream.Seek(0x30, SeekOrigin.Begin);
            Header.PatchID = Handle.ReadUInt16();
            Handle.BaseStream.Seek(0x44, SeekOrigin.Begin);
            Header.EntryTableOffset = Handle.ReadUInt32();
            Handle.BaseStream.Seek(0x60, SeekOrigin.Begin);
            Header.EntryTableCount = Handle.ReadUInt32();
        }

        public bool GetDynamicIndices()
        {
            GetHandle();
            ReadHeader();
            // Check entries until we find a dynamic. If not, return false
            DynamicHashIndices = new List<int>();
            Handle.BaseStream.Seek(Header.EntryTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.EntryTableCount; i++)
            {
                PkgEntry NewPkgEntry = new PkgEntry();
                byte[] buffer = new byte[0x10];
                Handle.BaseStream.Read(buffer, 0, 0x10);
                NewPkgEntry = StructConverter.ToStructure<PkgEntry>(buffer);
                if (NewPkgEntry.Reference == 0x80809AD8)
                {
                    DynamicHashIndices.Add(i);
                }
            }
            CloseHandle();
            if (DynamicHashIndices.Count > 0)
            {
                return true;
            }
            return false;
        }
    }
}
