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
        public bool Export(string PackagesPath, bool bBeyondLight)
        {
            string[] s = Path.Split("\\");
            string SavePath = String.Join("/", s);
            string SaveName = s.Last().Split(".")[0];

            [DllImport("DestinyDynamicExtractorBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);
            [DllImport("DestinyDynamicExtractorPREBL.dll", EntryPoint = "RequestExportDynamic")]
            static extern bool RequestExportDynamicPREBL([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);

            bool status = false;
            if (bBeyondLight)
            {
                status = RequestExportDynamicBL(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            }
            else
            {
                status = RequestExportDynamicPREBL(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            }

            return status;
        }
    }
}
