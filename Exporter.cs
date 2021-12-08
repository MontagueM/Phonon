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
        public bool Export(string PackagesPath)
        {
            string[] s = Path.Split("\\");
            string SavePath = String.Join("/", s);
            string SaveName = s.Last().Split(".")[0];
            [DllImport(@"DestinyDynamicExtractor.dll")]
            static extern bool RequestExportDynamic([MarshalAs(UnmanagedType.LPStr)] string DynamicHash, [MarshalAs(UnmanagedType.LPStr)] string pkgsPath, [MarshalAs(UnmanagedType.LPStr)] string ExportPath, [MarshalAs(UnmanagedType.LPStr)] string ExportName, bool bTextures);
            bool status = RequestExportDynamic(Hash, PackagesPath, SavePath, SaveName, bTextures); ;
            return status;
        }
    }
}
