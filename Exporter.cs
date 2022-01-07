using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Phonon
{
    public class Exporter
    {
        public string Path = "";
        public string Hash = "";
        public string SaveName = "";
        public PhononType ePhononType;
        public TextureFormat eTextureFormat;

        public Exporter()
        {

        }
        public bool Export(string PackagesPath)
        {
            string[] s = Path.Split("\\");
            string SavePath = String.Join("/", s);
            string FinalSaveName = "";
            if (SaveName == "")
            {
                SaveName = s.Last().Split(".")[0];
                FinalSaveName = SaveName;
            }
            else
            {
                SavePath += "/" + SaveName + "/";
                Directory.CreateDirectory(SavePath);
                FinalSaveName = SaveName +  "_" +  Hash;
            }


            [DllImport("DestinyDynamicExtractorBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, int TextureFormat);
            [DllImport("DestinyDynamicExtractorPREBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicPREBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, int TextureFormat);
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicD1([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, int TextureFormat);

            bool status = false;
            if (ePhononType == PhononType.Destiny2BL)
            {
                status = RequestExportDynamicBL(Hash, PackagesPath, SavePath, FinalSaveName, ((int)eTextureFormat));
            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                status = RequestExportDynamicPREBL(Hash, PackagesPath, SavePath, FinalSaveName, ((int)eTextureFormat));
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                status = RequestExportDynamicD1(Hash, PackagesPath, SavePath, FinalSaveName, ((int)eTextureFormat));
            }

            return status;
        }

        public bool ExportD1Map(string PackagesPath, List<string> MapNames, Dictionary<string, Dictionary<string, List<string>>> MapInfoDict)
        {
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestExportD1Map")]
            static extern bool RequestExportD1Map([MarshalAs(UnmanagedType.LPStr)] string MapHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, int TextureFormat);
            [DllImport("DestinyDynamicExtractorD1.dll", EntryPoint = "RequestExportD1DynMap")]
            static extern bool RequestExportD1DynMap([MarshalAs(UnmanagedType.LPStr)] string MapHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName);


            bool status = true;
            string[] s = Path.Split("\\");
            foreach (string MapName in MapNames)
            {
                string SavePath = String.Join("/", s) + "/" + MapName + "/";
                Directory.CreateDirectory(SavePath);
                string DynSavePath = $"{SavePath}/DynamicMaps/";
                Directory.CreateDirectory(DynSavePath);
                List<string> StaticHashes = MapInfoDict[MapName]["static"];
                foreach (string MapHash in StaticHashes)
                {
                    status &= RequestExportD1Map(MapHash, PackagesPath, SavePath, MapName, ((int)eTextureFormat));
                }

                List<string> DynHashes = MapInfoDict[MapName]["dynamic"];
                foreach (string MapHash in DynHashes)
                {
                    status &= RequestExportD1DynMap(MapHash, PackagesPath, DynSavePath, MapName);
                }
            }

            return status;
        }
    }
}
