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


    public struct PkgHeader
    {
        public ushort PkgID;
        public ushort PatchID;
        public uint EntryTableOffset;
        public uint EntryTableCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PkgEntry
    {
        public uint Reference;
        public uint EntryB;
        public uint EntryC;
        public uint EntryD;
    }

    public class Package
    {
        public string Path;
        public string Name;  // The name excludes the patch ID
        private BinaryReader Handle;
        public PkgHeader Header;
        private List<int> DynamicHashIndices;
        public List<Dynamic> Dynamics;
        public PhononType ePhononType;

        public Package(PhononType ePhononType)
        {
            this.ePhononType = ePhononType;
        }
        public Package(string PkgPath, PhononType ePhononType)
        {
            Path = PkgPath;
            this.ePhononType = ePhononType;
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
                bool success = dynamic.GetDynamicInfo(PackagesPath, ePhononType);
                if (success)
                {
                    Dynamics.Add(dynamic);
                }
            }
        }

        public bool GetHandle()
        {
            Handle = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read));
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
            if (ePhononType == PhononType.Destiny2BL)
            {
                Handle.BaseStream.Seek(0x10, SeekOrigin.Begin);
                Header.PkgID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0x30, SeekOrigin.Begin);
                Header.PatchID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0x44, SeekOrigin.Begin);
                Header.EntryTableOffset = Handle.ReadUInt32();
                Handle.BaseStream.Seek(0x60, SeekOrigin.Begin);
                Header.EntryTableCount = Handle.ReadUInt32();
            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                Handle.BaseStream.Seek(0x04, SeekOrigin.Begin);
                Header.PkgID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0x20, SeekOrigin.Begin);
                Header.PatchID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0x110, SeekOrigin.Begin);
                Header.EntryTableOffset = Handle.ReadUInt32() + 96;
                Handle.BaseStream.Seek(0xB4, SeekOrigin.Begin);
                Header.EntryTableCount = Handle.ReadUInt32();
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                Handle.BaseStream.Seek(0x04, SeekOrigin.Begin);
                Header.PkgID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0x20, SeekOrigin.Begin);
                Header.PatchID = Handle.ReadUInt16();
                Handle.BaseStream.Seek(0xB8, SeekOrigin.Begin);
                Header.EntryTableOffset = Handle.ReadUInt32();
                Handle.BaseStream.Seek(0xB4, SeekOrigin.Begin);
                Header.EntryTableCount = Handle.ReadUInt32();
            }
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
                uint DynamicRef = 0;
                if (ePhononType == PhononType.Destiny2BL)
                {
                    DynamicRef = 0x80809AD8;
                }
                else if (ePhononType == PhononType.Destiny2PREBL)
                {
                    DynamicRef = 0x80809C0F;
                }
                else if (ePhononType == PhononType.Destiny1)
                {
                    DynamicRef = 0x80800734;
                }
                if (NewPkgEntry.Reference == DynamicRef)
                {
                    //System.Diagnostics.Debug.WriteLine("i: " + i + " Ref: " + NewPkgEntry.Reference + " Pkg: " + Header.PkgID + " Pkg entry offset: " + Header.EntryTableOffset);
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
