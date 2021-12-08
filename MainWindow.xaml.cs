using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        ConcurrentDictionary<string, Package> Packages = new ConcurrentDictionary<string, Package>();
        Exporter ExportSettings = new Exporter();
        public MainWindow()
        {
            InitializeComponent();
            // Check if we need to get the package path first
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            if (config.AppSettings.Settings["PackagesPath"] != null)
            {
                SelectPkgsDirectoryButton.Visibility = Visibility.Hidden;
                LoadPackageList();
                ShowPackageList();
            }
            else
            {
                System.Windows.MessageBox.Show("No package path found");
            }
        }

        private void SelectPkgsDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                bool success = SetPackagePath(dialog.SelectedPath);
                if (success)
                {
                    SelectPkgsDirectoryButton.Visibility = Visibility.Hidden;
                    LoadPackageList();
                    ShowPackageList();
                }
            }
        }

        private void PkgButton_Click(object sender, RoutedEventArgs e)
        {
            string ClickedPackageName = (((sender as ToggleButton).Content) as TextBlock).Text;
            Package pkg = Packages[ClickedPackageName];
            ShowDynamicList(pkg);
        }

        private void ShowDynamicList(Package pkg)
        {
            if (pkg.Dynamics.Count == 0)
            {
                System.Windows.MessageBox.Show("Package " + pkg.Name + " has no valid dynamics");
                return;
            }

            PrimaryList.Children.Clear();

            // Go back
            ToggleButton btn = new ToggleButton();
            btn.Content = "Go back to package list";
            btn.Padding = new Thickness(10, 5, 0, 5);
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
            btn.HorizontalContentAlignment = HorizontalAlignment.Left;
            btn.Click += GoBack_Click;
            PrimaryList.Children.Add(btn);

            foreach (Dynamic dynamic in pkg.Dynamics)
            {
                btn = new ToggleButton();
                btn.Content = new TextBlock
                {
                    Text = dynamic.GetHashString() + "\nHas Skeleton: " + dynamic.bHasSkeleton.ToString() + "\nMesh Count: " + dynamic.MeshCount.ToString(),
                    TextWrapping = TextWrapping.Wrap,
                };
                Style style = Application.Current.Resources["ButtonStyle"] as Style;
                //btn.Style = style;
                btn.Height = 40;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                btn.HorizontalContentAlignment = HorizontalAlignment.Left;
                btn.Click += Dynamic_Click;
                PrimaryList.Children.Add(btn);
            }
            ScrollView.ScrollToTop();
        }

        private void Dynamic_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToggleButton button in PrimaryList.Children)
            {
                button.IsChecked = false;
            }
            (sender as ToggleButton).IsChecked = true;
            string ClickedDynamicHash = (((sender as ToggleButton).Content) as TextBlock).Text.Split("\n")[0];
            uint h = Convert.ToUInt32(ClickedDynamicHash, 16);
            ExportSettings.Hash = ClickedDynamicHash;
            Dynamic dynamic = new Dynamic(h);
            dynamic.GetDynamicMesh(GetPackagesPath());
            MainViewModel MVM = (MainViewModel)Wind.Resources["MVM"];
            MVM.UpdateModel(dynamic.Vertices, dynamic.Faces);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPackageList();
        }

        private void LoadPackageList()
        {
            if (File.Exists("packages.dat"))
            {
                bool success = ParsePackageList();
                if (!success) // TODO properly implement this, currently always returns true
                {
                    System.Windows.MessageBox.Show("Manifest change detected, rebuilding data file.");
                    GeneratePackageList();
                    SavePackageList();
                }
            }
            else
            {
                GeneratePackageList();
                SavePackageList();
                Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
                config.Save(ConfigurationSaveMode.Minimal);
            }
        }
        private bool ParsePackageList()
        {
            BinaryReader Handle = new BinaryReader(File.Open("packages.dat", FileMode.Open));
            // PkgID : Path dict
            IDictionary<int, Package> PackagePaths = new Dictionary<int, Package>();
            string[] files = Directory.GetFiles(GetPackagesPath(), "*.pkg", SearchOption.TopDirectoryOnly);
            // First get the dictionary of name : highest patch pkg
            foreach (string file in files)
            {
                Package pkg = new Package(file);
                if (!PackagePaths.ContainsKey(pkg.Header.PkgID))
                {
                    PackagePaths.Add(pkg.Header.PkgID, pkg);
                }
                else
                {
                    if (pkg.Header.PatchID > PackagePaths[pkg.Header.PkgID].Header.PatchID)
                    {
                        PackagePaths[pkg.Header.PkgID] = pkg;
                    }
                }
            }

            while (Handle.BaseStream.Position != Handle.BaseStream.Length)
            {
                Package pkg = new Package();
                pkg.Header.PkgID = Handle.ReadUInt16();
                pkg.Header.PatchID = Handle.ReadUInt16();
                ushort DynamicCount = Handle.ReadUInt16();
                if (DynamicCount == 0)
                {
                    continue;
                }
                pkg.Dynamics = new List<Dynamic>();
                for (int i = 0; i < DynamicCount; i++)
                {
                    uint DynamicHash = Handle.ReadUInt32();
                    Dynamic dynamic = new Dynamic(DynamicHash);
                    dynamic.MeshCount = Handle.ReadUInt16();
                    dynamic.bHasSkeleton = Handle.ReadBoolean();
                    pkg.Dynamics.Add(dynamic);
                }
                // Get package path
                pkg.Path = PackagePaths[pkg.Header.PkgID].Path;
                Packages.AddOrUpdate(pkg.MakeName(), pkg, (Key, OldValue) => OldValue);
            }
            Handle.Close();
            return true;
        }

        private void GeneratePackageList()
        {
            string[] files = Directory.GetFiles(GetPackagesPath(), "*.pkg", SearchOption.TopDirectoryOnly);
            // First get the dictionary of name : highest patch pkg
            foreach (string file in files)
            {
                if (!file.EndsWith(".pkg")) continue;
                Package pkg = new Package(file);
                if (!Packages.ContainsKey(pkg.Name))
                {
                    Packages.AddOrUpdate(pkg.Name, pkg, (Key, OldValue) => OldValue);
                }
                else
                {
                    if (pkg.Header.PatchID > Packages[pkg.Name].Header.PatchID)
                    {
                        Packages[pkg.Name] = pkg;
                    }
                }
            }
            foreach (Package pkg in Packages.Values.ToList())
            {
                ThreadPool.QueueUserWorkItem(ThreadProc, new object[] { pkg });
                //ThreadProc(pkg);
            }
            int workerThreads = 0; int completionPortThreads = 0; int maxWorkerThreads = 0; int maxCompletionPortThreads = 0;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            while (workerThreads != maxWorkerThreads)
            {
                ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                continue;
            }
            System.Diagnostics.Debug.WriteLine("Finished generation");
        }

        private void SavePackageList()
        {
            BinaryWriter Handle = new BinaryWriter(File.Open("packages.dat", FileMode.Create));
            foreach (Package pkg in Packages.Values)
            {
                Handle.Write(pkg.Header.PkgID);
                Handle.Write(pkg.Header.PatchID);
                if (pkg.Dynamics == null)
                {
                    Handle.Write((ushort)0);
                }
                else
                {
                    Handle.Write((ushort)pkg.Dynamics.Count);
                    foreach (Dynamic dyn in pkg.Dynamics)
                    {
                        Handle.Write((uint)dyn.Hash);
                        Handle.Write((ushort)dyn.MeshCount);
                        Handle.Write((bool)dyn.bHasSkeleton);
                    }
                }
            }
            Handle.Close();
        }

        private void ThreadProc(object state)
        //private void ThreadProc(Package pkg)
        {
            object[] array = state as object[];
            Package pkg = (Package)array[0];
            //if (pkg.Header.PkgID != 0x2f0)
            //{
            //    Packages.TryRemove(pkg.Name, out pkg);
            //    return;
            //}
            if (!pkg.GetDynamicIndices())
            {
                Packages.TryRemove(pkg.Name, out pkg);
                return;
            }
            pkg.GetDynamics(GetPackagesPath());
            System.Diagnostics.Debug.WriteLine("Package " + pkg.Name);
            if (pkg.Dynamics.Count == 0)
            {
                Packages.TryRemove(pkg.Name, out pkg);
            }
        }

        private void ShowPackageList()
        {
            PrimaryList.Children.Clear();
            List<string> PackageNames = Packages.Keys.ToList<string>();
            PackageNames.Sort();
            foreach (string PkgName in PackageNames)
            {
                // We want to verify that this pkg has at least 1 dynamic model in it.
                ToggleButton btn = new ToggleButton();
                btn.Content = new TextBlock
                {
                    Text = PkgName,
                    TextWrapping = TextWrapping.Wrap,
                }; ;
                //Style style = Application.Current.Resources["ButtonStyle"] as Style;
                //btn.Style = style;
                //btn.Padding = new Thickness(10, 10, 0, 10);
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                //btn.MouseEnter += ButtonEnter; // this doesnt work for some reason
                //btn.MouseLeave += ButtonLeave;
                btn.Height = 40;
                btn.Click += PkgButton_Click;
                PrimaryList.Children.Add(btn);
            }
            ScrollView.ScrollToTop();
        }

        private void ButtonEnter(object sender, MouseEventArgs e)
        {
            (sender as ToggleButton).Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
        }

        private void ButtonLeave(object sender, MouseEventArgs e)
        {
            (sender as ToggleButton).Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
        }

        private bool SetPackagePath(string Path)
        {
            // Verify this is a valid path by checking to see if a .pkg file is inside
            string[] files = Directory.GetFiles(Path, "*.pkg", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                System.Windows.MessageBox.Show("Directory selected is invalid, please select the correct packages directory.");
                return false;
            }
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            config.AppSettings.Settings.Remove("PackagesPath");
            config.AppSettings.Settings.Add("PackagesPath", Path);
            config.Save(ConfigurationSaveMode.Minimal);
            return true;
        }

        public string GetPackagesPath()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            return config.AppSettings.Settings["PackagesPath"].Value.ToString();
        }

        // Menu buttons

        private void Grid_Checked(object sender, RoutedEventArgs e)
        {
            if (HelixGrid != null)
            {
                HelixGrid.Visible = true;
            }
        }

        private void Grid_Unchecked(object sender, RoutedEventArgs e)
        {
            if (HelixGrid != null)
            {
                HelixGrid.Visible = false;
            }
        }

        private void ExportTextures_Checked(object sender, RoutedEventArgs e)
        {
            ExportSettings.bTextures = true;
        }

        private void ExportTextures_Unchecked(object sender, RoutedEventArgs e)
        {
            ExportSettings.bTextures = false;
        }

        private void SetExportPath_Clicked(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                bool success = SetExportPath(dialog.SelectedPath);
                if (success)
                {
                    System.Windows.MessageBox.Show("Export path successfully set");
                }
            }
        }

        private bool SetExportPath(string Path)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            config.AppSettings.Settings.Remove("ExportPath");
            config.AppSettings.Settings.Add("ExportPath", Path);
            config.Save(ConfigurationSaveMode.Minimal);
            return true;
        }

        private void Export_Clicked(object sender, RoutedEventArgs e)
        {
            if (ExportSettings.Hash == "")
            {
                System.Windows.MessageBox.Show("No dynamic model selected");
                return;
            }
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.Filter = "Model files | *.fbx";
                dialog.DefaultExt = "fbx";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                ExportSettings.Path = dialog.FileName;
            }
            bool status = ExportSettings.Export(GetPackagesPath());
            if (status)
            {
                System.Windows.MessageBox.Show("Export success");
            }
            else
            {
                System.Windows.MessageBox.Show("Export failed");
            }
        }
    }

}
