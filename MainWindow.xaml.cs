using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Phonon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum PhononType
    {
        [Description("Destiny1")]
        Destiny1 = 1,
        [Description("Destiny2PREBL")]
        Destiny2PREBL = 2,
        [Description("Destiny2BL")]
        Destiny2BL = 3,
    }

    public enum TextureFormat
    {
        [Description("None")]
        None = 1,
        [Description("DDS")]
        DDS = 2,
        [Description("TGA")]
        TGA = 3,
        [Description("PNG")]
        PNG = 4,
    }

    public partial class MainWindow : Window
    {
        public Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
        public PhononType ePhononType;
        public string PkgCacheName;
        public string PkgPathKey;
        public Exporter ExportSettings = new Exporter();

        public void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            InitialiseConfig();
        }

        private void InitialiseConfig()
        {
            if (config.AppSettings.Settings["Version"] != null)
            {
                if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny2BL.ToString())
                {
                    SetBL();
                    Destiny2BL.IsChecked = true;

                }
                else if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny2PREBL.ToString())
                {
                    SetPREBL();
                    Destiny2PREBL.IsChecked = true;
                }
                else if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny1.ToString())
                {
                    SetD1();
                    Destiny1.IsChecked = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect value set for 'Version', defaulting to Beyond Light settings");
                    SetBL();
                    Destiny2BL.IsChecked = true;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Defaulting to Beyond Light settings");
                SetBL();
                Destiny2BL.IsChecked = true;
                config.AppSettings.Settings.Add("Version", PhononType.Destiny2BL.ToString());
                config.Save(ConfigurationSaveMode.Minimal);
            }
            ExportSettings.ePhononType = ePhononType;

            if (config.AppSettings.Settings["TextureFormat"] != null)
            {
                if (config.AppSettings.Settings["TextureFormat"].Value == TextureFormat.None.ToString())
                {
                    None.IsChecked = true;
                    ExportSettings.eTextureFormat = TextureFormat.None;
                }
                else if (config.AppSettings.Settings["TextureFormat"].Value == TextureFormat.DDS.ToString())
                {
                    DDS.IsChecked = true;
                    ExportSettings.eTextureFormat = TextureFormat.DDS;
                }
                else if (config.AppSettings.Settings["TextureFormat"].Value == TextureFormat.TGA.ToString())
                {
                    TGA.IsChecked = true;
                    ExportSettings.eTextureFormat = TextureFormat.TGA;
                }
                else if (config.AppSettings.Settings["TextureFormat"].Value == TextureFormat.PNG.ToString())
                {
                    PNG.IsChecked = true;
                    ExportSettings.eTextureFormat = TextureFormat.PNG;
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect value set for 'TextureFormat', defaulting to None");
                    None.IsChecked = true;
                    ExportSettings.eTextureFormat = TextureFormat.None;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Defaulting to no texture extraction");
                None.IsChecked = true;
                ExportSettings.eTextureFormat = TextureFormat.None;
                config.AppSettings.Settings.Add("TextureFormat", TextureFormat.None.ToString());
                config.Save(ConfigurationSaveMode.Minimal);
            }
        }

        private void ChangeTextureFormat(object sender, RoutedEventArgs e)
        {
            if (config.AppSettings.Settings["TextureFormat"] == null)
            {
                config.AppSettings.Settings.Add("TextureFormat", "None");
                config.Save(ConfigurationSaveMode.Minimal);
            }


            RadioButton rb = sender as RadioButton;
            TextureFormat TargetFormat;
            switch (rb.Name)
            {
                case "None":
                    TargetFormat = TextureFormat.None;
                    break;
                case "DDS":
                    TargetFormat = TextureFormat.DDS;
                    break;
                case "TGA":
                    TargetFormat = TextureFormat.TGA;
                    break;
                case "PNG":
                    TargetFormat = TextureFormat.PNG;
                    break;
                default:
                    TargetFormat = TextureFormat.None;
                    break;
            }
            config.AppSettings.Settings["TextureFormat"].Value = TargetFormat.ToString();
            config.Save(ConfigurationSaveMode.Minimal);
            ExportSettings.eTextureFormat = TargetFormat;
        }

        private void ChangeVersion(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (config.AppSettings.Settings["Version"] == null) return;

            switch (rb.Name)
            {
                case "Destiny1":
                    SetD1();
                    break;
                case "Destiny2PREBL":
                    SetPREBL();
                    break;
                case "Destiny2BL":
                    SetBL();
                    break;
                default:
                    break;
            }
            config.Save(ConfigurationSaveMode.Minimal);
            ExportSettings.ePhononType = ePhononType;
            ConsiderShowD1Tabs();
            Dispatcher.BeginInvoke((Action)(() => MainTabControl.SelectedIndex = 0));

            ModelView mv = FindName("ModelViewItem") as ModelView;
            if (mv.Packages.Count > 0)
            {
                mv.Packages.Clear();

                if (config.AppSettings.Settings[PkgPathKey] != null)
                {
                    mv.LoadPackageList();
                    mv.ShowPackageList();
                }
                else
                {
                    System.Windows.MessageBox.Show($"No package path found for {ePhononType.ToString()}");
                    mv.SelectPkgsDirectoryButton_Click(sender, e);
                }
            }
        }
        private void SetD1()
        {
            config.AppSettings.Settings["Version"].Value = PhononType.Destiny1.ToString();
            ePhononType = PhononType.Destiny1;
            Wind.Title = "Phonon D1";
            PkgPathKey = "PackagesPathD1";
            PkgCacheName = "packagesD1.dat";
        }
        private void SetPREBL()
        {
            config.AppSettings.Settings["Version"].Value = PhononType.Destiny2PREBL.ToString();
            ePhononType = PhononType.Destiny2PREBL;
            Wind.Title = "Phonon PRE-BL";
            PkgPathKey = "PackagesPathPREBL";
            PkgCacheName = "packagesPREBL.dat";
        }
        private void SetBL()
        {
            config.AppSettings.Settings["Version"].Value = PhononType.Destiny2BL.ToString();
            ePhononType = PhononType.Destiny2BL;
            Wind.Title = "Phonon BL";
            PkgPathKey = "PackagesPathBL";
            PkgCacheName = "packagesBL.dat";
        }
        private void ConsiderShowD1Tabs()
        {
            if (ePhononType == PhononType.Destiny1)
            {
                MapViewTab.Visibility = Visibility.Visible;
                APIViewTab.Visibility = Visibility.Visible;
            }
            else
            {
                MapViewTab.Visibility = Visibility.Hidden;
                APIViewTab.Visibility = Visibility.Hidden;
            }
        }
    }
}
