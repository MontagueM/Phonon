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

    public partial class ModelView : UserControl
    {
        ConcurrentDictionary<string, Package> Packages = new ConcurrentDictionary<string, Package>();
        List<Dynamic> CurrentDynamics = new List<Dynamic>();
        string CurrentPkg = "";
        Exporter ExportSettings = new Exporter();
        PhononType ePhononType;
        string PkgPathKey = "";
        string PkgCacheName = "";
        private MainWindow mainWindow = null; // Reference to the MainWindow
        int SelectedDynamicIndex = 0;

        public ModelView()
        {
            InitializeComponent();
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Window.GetWindow(this) as MainWindow;
            if (PkgCacheName == "")
            {
                InitialiseConfig();
            }
        }

        private void InitialiseConfig()
        {
            // Check if we need to get the package path first
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            // Check what version to load (BL or pre-BL)

            if (config.AppSettings.Settings["Version"] != null)
            {
                if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny2BL.ToString())
                {
                    ePhononType = PhononType.Destiny2BL;
                    mainWindow.Wind.Title = "Phonon BL";
                    PkgPathKey = "PackagesPathBL";
                    PkgCacheName = "packagesBL.dat";
                    Destiny2BL.IsChecked = true;

                }
                else if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny2PREBL.ToString())
                {
                    ePhononType = PhononType.Destiny2PREBL;
                    mainWindow.Wind.Title = "Phonon PRE-BL";
                    PkgPathKey = "PackagesPathPREBL";
                    PkgCacheName = "packagesPREBL.dat";
                    Destiny2PreBL.IsChecked = true;
                }
                else if (config.AppSettings.Settings["Version"].Value == PhononType.Destiny1.ToString())
                {
                    ePhononType = PhononType.Destiny1;
                    mainWindow.Wind.Title = "Phonon D1";
                    PkgPathKey = "PackagesPathD1";
                    PkgCacheName = "packagesD1.dat";
                    Destiny1.IsChecked = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect value set for 'Version', defaulting to Beyond Light settings");
                    ePhononType = PhononType.Destiny2BL;
                    mainWindow.Wind.Title = "Phonon BL";
                    PkgPathKey = "PackagesPathBL";
                    PkgCacheName = "packagesBL.dat";
                    Destiny2BL.IsChecked = true;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Defaulting to Beyond Light settings");
                ePhononType = PhononType.Destiny2BL;
                mainWindow.Wind.Title = "Phonon BL";
                PkgPathKey = "PackagesPathBL";
                PkgCacheName = "packagesBL.dat";
                Destiny2BL.IsChecked = true;
                config.AppSettings.Settings.Add("Version", PhononType.Destiny2BL.ToString());
                config.Save(ConfigurationSaveMode.Minimal);
            }

            // Check for package path and load the list
            if (config.AppSettings.Settings[PkgPathKey] != null)
            {
                SelectPkgsDirectoryButton.Visibility = Visibility.Hidden;
                LoadPackageList();
                ShowPackageList();
            }
            else
            {
                System.Windows.MessageBox.Show($"No package path found for {ePhononType.ToString()}");
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
            CurrentDynamics.Clear();
            string ClickedPackageName = (((sender as ToggleButton).Content) as TextBlock).Text;
            Package pkg = Packages[ClickedPackageName];
            CurrentPkg = ClickedPackageName;
            CurrentDynamics = pkg.Dynamics;
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
            Style style = Application.Current.Resources["Button_Command"] as Style;
            btn.Style = style;
            btn.HorizontalAlignment = HorizontalAlignment.Stretch;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.Height = 40;
            btn.Focusable = false;
            btn.Content = "Go back to package list";
            btn.Padding = new Thickness(10, 5, 0, 5);
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
            btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            btn.HorizontalContentAlignment = HorizontalAlignment.Left;
            btn.Click += GoBack_Click;


            PrimaryList.Children.Add(btn);
            foreach (Dynamic dynamic in pkg.Dynamics)
            {
                btn = new ToggleButton();
                btn.Focusable = true;

                btn.Content = new TextBlock
                {
                    Text = dynamic.GetHashString() + "\nHas Skeleton: " + dynamic.bHasSkeleton.ToString() + "\nMesh Count: " + dynamic.MeshCount.ToString(),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                };

                btn.Style = style;
                btn.Height = 70;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                btn.HorizontalContentAlignment = HorizontalAlignment.Left;
                btn.Click += Dynamic_Click;

                //if (PrimaryList.Children.Count == 1)
                //{
                //    btn.Focus();
                //    Dynamic_Click(btn, new RoutedEventArgs());
                //}

                PrimaryList.Children.Add(btn);
            }

            ScrollView.ScrollToTop();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Right)
            {
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = false;
                SelectedDynamicIndex++;
                PrimaryList.Children[SelectedDynamicIndex].Focus();
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = true;
                Dynamic_Click(PrimaryList.Children[SelectedDynamicIndex], new RoutedEventArgs());
            }
            else if (e.Key == Key.Up || e.Key == Key.Left)
            {
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = false;
                SelectedDynamicIndex--;
                PrimaryList.Children[SelectedDynamicIndex].Focus();
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = true;
                Dynamic_Click(PrimaryList.Children[SelectedDynamicIndex], new RoutedEventArgs());
            }
        }

        private void Dynamic_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToggleButton button in PrimaryList.Children)
            {
                button.IsChecked = false;
            }
            SelectedDynamicIndex = PrimaryList.Children.IndexOf((sender as ToggleButton));
            (sender as ToggleButton).IsChecked = true;
            string ClickedDynamicHash = (((sender as ToggleButton).Content) as TextBlock).Text.Split("\n")[0];
            System.Diagnostics.Debug.WriteLine($"Clicked {ClickedDynamicHash}");

            File.AppendAllText("debug_phonon.log", $"Clicked { ClickedDynamicHash}" + Environment.NewLine);
            uint h = Convert.ToUInt32(ClickedDynamicHash, 16);
            ExportSettings.Hash = ClickedDynamicHash;
            Dynamic dynamic = new Dynamic(h);
            dynamic.GetDynamicMesh(GetPackagesPath(), ePhononType);
            MainViewModel MVM = (MainViewModel)UCModelView.Resources["MVM"];
            MVM.UpdateModel(dynamic.Vertices, dynamic.Faces);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPackageList();
        }

        private void LoadPackageList()
        {
            if (File.Exists(PkgCacheName))
            {
                bool success = ParsePackageList();
                if (!success)
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
            BinaryReader Handle = new BinaryReader(File.Open(PkgCacheName, FileMode.Open));
            // PkgID : Path dict
            IDictionary<int, Package> PackagePaths = new Dictionary<int, Package>();
            string[] files = Directory.GetFiles(GetPackagesPath(), "*.pkg", SearchOption.TopDirectoryOnly);
            // First get the dictionary of name : highest patch pkg

            foreach (string file in files)
            {
                Package pkg = new Package(file, ePhononType);
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
                Package pkg = new Package(ePhononType);
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
                if (!file.EndsWith(".pkg") || file.Contains("audio") || file.Contains("investment")) continue;
                Package pkg = new Package(file, ePhononType);
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
            //foreach (Package pkg in Packages.Values.ToList())
            Parallel.ForEach(Packages.Values.ToList(), pkg =>
            {
                //ThreadPool.QueueUserWorkItem(ThreadProc, new object[] { pkg });
                ThreadProc(pkg);
            });

            System.Diagnostics.Debug.WriteLine("Finished generation");
        }

        private void SavePackageList()
        {
            BinaryWriter Handle = new BinaryWriter(File.Open(PkgCacheName, FileMode.Create));
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

        //private void ThreadProc(object state)
        private void ThreadProc(Package pkg)
        {
            //object[] array = state as object[];
            //Package pkg = (Package)array[0];
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
                Style style = Application.Current.Resources["Button_Command"] as Style;

                btn.Style = style;
                btn.HorizontalAlignment = HorizontalAlignment.Stretch;
                btn.VerticalAlignment = VerticalAlignment.Center;
                btn.Focusable = true;
                btn.Content = new TextBlock
                {
                    Text = PkgName,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                }; ;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                btn.Height = 50;
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
            config.AppSettings.Settings.Remove(PkgPathKey);
            config.AppSettings.Settings.Add(PkgPathKey, Path);
            config.Save(ConfigurationSaveMode.Minimal);
            return true;
        }

        public string GetPackagesPath()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            return config.AppSettings.Settings[PkgPathKey].Value.ToString();
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

        private void HandleVersionCheck(object sender, RoutedEventArgs e) //Changes Version based on selected radio button
        {
            RadioButton rb = sender as RadioButton;
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);

            switch (rb.Name)
            {
                case "Destiny1":
                    config.AppSettings.Settings["Version"].Value = PhononType.Destiny1.ToString();
                    ePhononType = PhononType.Destiny1;
                    mainWindow.Wind.Title = "Phonon D1";
                    PkgPathKey = "PackagesPathD1";
                    PkgCacheName = "packagesD1.dat";
                    break;

                case "Destiny2BL":
                    config.AppSettings.Settings["Version"].Value = PhononType.Destiny2BL.ToString();
                    ePhononType = PhononType.Destiny2BL;
                    mainWindow.Wind.Title = "Phonon BL";
                    PkgPathKey = "PackagesPathBL";
                    PkgCacheName = "packagesBL.dat";
                    break;

                case "Destiny2PreBL":
                    config.AppSettings.Settings["Version"].Value = PhononType.Destiny2PREBL.ToString();
                    ePhononType = PhononType.Destiny2PREBL;
                    mainWindow.Wind.Title = "Phonon PRE-BL";
                    PkgPathKey = "PackagesPathPREBL";
                    PkgCacheName = "packagesPREBL.dat";
                    break;

                default:

                    break;
            }

            config.Save(ConfigurationSaveMode.Minimal);

            Packages.Clear();

            if (config.AppSettings.Settings[PkgPathKey] != null)
            {
                LoadPackageList();
                ShowPackageList();
            }
            else
            {
                System.Windows.MessageBox.Show($"No package path found for {ePhononType.ToString()}");
                SelectPkgsDirectoryButton_Click(sender, e);
            }


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
            bool status = ExportSettings.Export(GetPackagesPath(), ePhononType);
            if (status)
            {
                System.Windows.MessageBox.Show("Export success");
            }
            else
            {
                System.Windows.MessageBox.Show("Export failed");
            }
        }

        private void ExportAll_Clicked(object sender, RoutedEventArgs e)
        {

            if (CurrentDynamics.Count == 0)
            {
                System.Windows.MessageBox.Show("No PKG selected");
                return;
            }

            string outputpath;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                outputpath = dialog.SelectedPath;
            }
            if (outputpath == "")
            {
                System.Windows.MessageBox.Show("No output path selected");
                return;
            }

            foreach (Dynamic dynamic in CurrentDynamics)
            {
                ExportSettings.Hash = dynamic.HashString;
                ExportSettings.Path = $"{outputpath}\\{CurrentPkg}\\{dynamic.Hash.ToString()}.fbx";
                bool status = ExportSettings.Export(GetPackagesPath(), ePhononType);
            }
            System.Windows.MessageBox.Show("Export success");
        }
    }

}
