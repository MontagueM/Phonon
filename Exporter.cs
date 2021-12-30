using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Phonon
{
    class Exporter
    {
        public bool bTextures = true;
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
                status = RequestExportDynamicBL(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            }
            else if (ePhononType == PhononType.Destiny2PREBL)
            {
                status = RequestExportDynamicPREBL(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            }
            else if (ePhononType == PhononType.Destiny1)
            {
                status = RequestExportDynamicD1(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            }

            return status;
        }
    }
}
