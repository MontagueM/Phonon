using System;
using System.Collections.Generic;
using System.IO;
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
        public List<float[]> Vertices;
        public List<uint[]> Faces;

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

        public string GetHashString()
        {
            HashString = LittleEndian(Hash);
            return HashString;
        }

        public bool GetDynamicInfo(string PackagesPath)
        {
            const string DLLPath = "C:/Users/monta/OneDrive/Destiny 2 Datamining/CPP/DestinyDataminingCPP/x64/Debug/DestinyDynamicExtractor.dll";
            [DllImport(DLLPath)]
            static extern bool RequestDynamicInformation(string DynamicHash, string pkgsPath, ref int MeshCount, ref bool bHasSkeleton);

            GetHashString();
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

        public bool GetDynamicMesh(string PackagesPath)
        {
            Vertices = new List<float[]>();
            Faces = new List<uint[]>();
            const string DLLPath = "C:/Users/monta/OneDrive/Destiny 2 Datamining/CPP/DestinyDataminingCPP/x64/Debug/DestinyDynamicExtractor.dll";
            [DllImport(DLLPath)]
            static extern bool RequestSaveDynamicMeshData(string DynamicHash, string pkgsPath);
            var a = Hash.ToString("X");
            RequestSaveDynamicMeshData(Hash.ToString("X"), PackagesPath);
            return ReadMeshData(Vertices, Faces);
        }

        public bool ReadMeshData(List<float[]> Vertices, List<uint[]> Faces)
        {
            BinaryReader Handle = new BinaryReader(File.Open("msh.tmp", FileMode.Open));
            // We'll import it as one big mesh
            uint FaceCounter = 0;
            int q = 0;
            while (Handle.BaseStream.Position != Handle.BaseStream.Length)
            {
                uint VertexCount = Handle.ReadUInt32();
                for (int i = 0; i < VertexCount; i++)
                {
                    float[] Vertex = new float[3];
                    for (int j = 0; j < 3; j++)
                    {
                        Vertex[j] = Handle.ReadSingle();
                    }
                    Vertices.Add(Vertex);
                }
                uint FaceCount = Handle.ReadUInt32();
                for (int i = 0; i < FaceCount; i++)
                {
                    uint[] Face = new uint[3];
                    for (int j = 0; j < 3; j++)
                    {
                        uint f = Handle.ReadUInt32();
                        Face[j] = FaceCounter + f; // Account for it being one mesh
                    }
                    Faces.Add(Face);
                }
                FaceCounter += VertexCount;
                //if (q++ == 1) break;
            }
            Handle.Close();
            return true;
        }
    }
}
