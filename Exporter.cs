using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Phonon
{
    class Exporter
    {
        public bool bTextures = false;
        public string Path = "";
        public string Hash = "";

        public Exporter()
        {

        }
        public bool Export(string PackagesPath, PhononType ePhononType)
        {
            string[] s = Path.Split("\\");
            string SavePath = String.Join("/", s);
            string SaveName = s.Last().Split(".")[0];

            [DllImport("DestinyDynamicExtractorBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);
            [DllImport("DestinyDynamicExtractorPREBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicPREBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicD1([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);

            bool status = false;
            if (ePhononType == PhononType.Destiny2BL)
            {
                status = RequestExportDynamicBL(Hash, PackagesPath, SavePath, SaveName, bTextures);
            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                status = RequestExportDynamicPREBL(Hash, PackagesPath, SavePath, SaveName, bTextures);
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                status = RequestExportDynamicD1(Hash, PackagesPath, SavePath, SaveName, bTextures);
            }

            return status;
        }

        public bool ExportD1Map(string PackagesPath, List<string> MapNames, Dictionary<string, Dictionary<string, List<string>>> MapInfoDict)
        {
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestExportD1Map")]
            static extern bool RequestExportD1Map([MarshalAs(UnmanagedType.LPStr)] string MapHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);

            bool status = true;
            string[] s = Path.Split("\\");
            //Parallel.ForEach(MapNames, MapName =>
            foreach (string MapName in MapNames)
            {
                string SavePath = String.Join("/", s) + "/" + MapName + "/";
                Directory.CreateDirectory(SavePath);
                List<string> StaticHashes = MapInfoDict[MapName]["static"];
                //Parallel.ForEach(StaticHashes, MapHash =>
                foreach (string MapHash in StaticHashes)
                {
                    status &= RequestExportD1Map(MapHash, PackagesPath, SavePath, MapName, bTextures);
                }//);

            }//);

            return status;
        }
    }
}
