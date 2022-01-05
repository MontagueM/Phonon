using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Phonon
{

    public class Dynamic
    {
        public uint Hash;
        public string HashString;
        public int MeshCount;
        public bool bHasSkeleton;
        public List<float[]> Vertices;
        public List<uint[]> Faces;

        public Dynamic(uint DynamicHash)
        {
            Hash = DynamicHash;
        }

        static string LittleEndian(uint number)
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

        public bool GetDynamicInfo(string PackagesPath, PhononType ePhononType)
        {
            GetHashString();
            // TODO do this on a separate thread as to not lag the UI
            bool status = false;

            [DllImport("DestinyDynamicExtractorBL.dll", EntryPoint="RequestDynamicInformation")]
            static extern bool RequestDynamicInformationBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, ref int MeshCount, ref bool bHasSkeleton);
            [DllImport("DestinyDynamicExtractorPREBL.dll", EntryPoint = "RequestDynamicInformation")]
            static extern bool RequestDynamicInformationPREBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, ref int MeshCount, ref bool bHasSkeleton);
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestDynamicInformation")]
            static extern bool RequestDynamicInformationD1([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, ref int MeshCount, ref bool bHasSkeleton);


            if (ePhononType == PhononType.Destiny2BL)
            {
                status = RequestDynamicInformationBL(HashString, PackagesPath, ref MeshCount, ref bHasSkeleton);
            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                status = RequestDynamicInformationPREBL(HashString, PackagesPath, ref MeshCount, ref bHasSkeleton);
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                status = RequestDynamicInformationD1(HashString, PackagesPath, ref MeshCount, ref bHasSkeleton);
            }
            return status && (MeshCount > 0);
        }

        public bool GetDynamicMesh(string PackagesPath, PhononType ePhononType)
        {
            Vertices = new List<float[]>();
            Faces = new List<uint[]>();
            [DllImport("DestinyDynamicExtractorBL.dll", EntryPoint = "RequestSaveDynamicMeshData")]
            static extern bool RequestSaveDynamicMeshDataBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath);
            [DllImport("DestinyDynamicExtractorPREBL.dll", EntryPoint = "RequestSaveDynamicMeshData")]
            static extern bool RequestSaveDynamicMeshDataPREBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath);
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestSaveDynamicMeshData")]
            static extern bool RequestSaveDynamicMeshDataD1([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath);

            var a = Hash.ToString("X8");
            bool status = false;
            if (ePhononType == PhononType.Destiny2BL)
            {
                status = RequestSaveDynamicMeshDataBL(a, PackagesPath);

            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                status = RequestSaveDynamicMeshDataPREBL(a, PackagesPath);
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                status = RequestSaveDynamicMeshDataD1(a, PackagesPath);
            }
            return ReadMeshData(Vertices, Faces) && status;
        }

        public bool ReadMeshData(List<float[]> Vertices, List<uint[]> Faces)
        {
            BinaryReader Handle = new BinaryReader(File.Open("msh.tmp", FileMode.Open));
            // We'll import it as one big mesh
            uint FaceCounter = 0;
            while (Handle.BaseStream.Position != Handle.BaseStream.Length)
            {
                uint VertexCount = 0;
                try
                {
                    VertexCount = Handle.ReadUInt32();
                }
                catch (System.IO.EndOfStreamException e)
                {
                    System.Windows.MessageBox.Show("Mesh file broken, deleting");
                    Handle.Close();
                    File.Delete("msh.tmp");
                    return false;
                }
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
