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
        public ConcurrentDictionary<string, Package> Packages = new ConcurrentDictionary<string, Package>();
        Package CurrentPkg;
        private MainWindow mainWindow = null; // Reference to the MainWindow
        int SelectedDynamicIndex = 0;

        public ModelView()
        {
            InitializeComponent();
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow.PkgCacheName != "")
            {
                InitialiseConfig();
            }
        }

        private void InitialiseConfig()
        {
            // Check for package path and load the list
            if (mainWindow.config.AppSettings.Settings[mainWindow.PkgPathKey] != null)
            {
                SelectPkgsDirectoryButton.Visibility = Visibility.Hidden;
                LoadPackageList();
                ShowPackageList();
            }
            else
            {
                System.Windows.MessageBox.Show($"No package path found for {mainWindow.ePhononType.ToString()}");
            }
        }

        public void SelectPkgsDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = $"Select the packages folder for {mainWindow.ePhononType.ToString()}";
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
            CurrentPkg = pkg;
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

                PrimaryList.Children.Add(btn);
            }

            ScrollView.ScrollToTop();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Down || e.Key == Key.Right) && SelectedDynamicIndex < PrimaryList.Children.Count - 1)
            {
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = false;
                SelectedDynamicIndex++;
                PrimaryList.Children[SelectedDynamicIndex].Focus();
                (PrimaryList.Children[SelectedDynamicIndex] as ToggleButton).IsChecked = true;
                Dynamic_Click(PrimaryList.Children[SelectedDynamicIndex], new RoutedEventArgs());
            }
            else if ((e.Key == Key.Up || e.Key == Key.Left) && SelectedDynamicIndex > 1)
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
            mainWindow.ExportSettings.Hash = ClickedDynamicHash;
            Dynamic dynamic = new Dynamic(h);
            dynamic.GetDynamicMesh(GetPackagesPath(), mainWindow.ePhononType);
            MainViewModel MVM = (MainViewModel)UCModelView.Resources["MVM"];
            MVM.UpdateModel(dynamic.Vertices, dynamic.Faces);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPackageList();
        }

        public void LoadPackageList()
        {
            if (File.Exists(mainWindow.PkgCacheName))
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
                config.Save(ConfigurationSaveMode.Modified);
            }
        }
        private bool ParsePackageList()
        {
            BinaryReader Handle = new BinaryReader(File.Open(mainWindow.PkgCacheName, FileMode.Open));
            // PkgID : Path dict
            IDictionary<int, Package> PackagePaths = new Dictionary<int, Package>();
            string[] files = Directory.GetFiles(GetPackagesPath(), "*.pkg", SearchOption.TopDirectoryOnly);
            // First get the dictionary of name : highest patch pkg

            foreach (string file in files)
            {
                Package pkg = new Package(file, mainWindow.ePhononType);
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
                Package pkg = new Package(mainWindow.ePhononType);
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
                Package pkg = new Package(file, mainWindow.ePhononType);
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
            Parallel.ForEach(Packages.Values.ToList(), pkg =>
            {
                ThreadProc(pkg);
            });

            System.Diagnostics.Debug.WriteLine("Finished generation");
        }

        private void SavePackageList()
        {
            BinaryWriter Handle = new BinaryWriter(File.Open(mainWindow.PkgCacheName, FileMode.Create));
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

        private void ThreadProc(Package pkg)
        {
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

        public void ShowPackageList()
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
            if (Path == "")
            {
                System.Windows.MessageBox.Show("Directory selected is invalid, please select the correct packages directory.");
                return false;
            }
            // Verify this is a valid path by checking to see if a .pkg file is inside
            string[] files = Directory.GetFiles(Path, "*.pkg", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                System.Windows.MessageBox.Show("Directory selected is invalid, please select the correct packages directory.");
                return false;
            }
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            config.AppSettings.Settings.Remove(mainWindow.PkgPathKey);
            config.AppSettings.Settings.Add(mainWindow.PkgPathKey, Path);
            config.Save(ConfigurationSaveMode.Modified);
            return true;
        }

        public string GetPackagesPath()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            return config.AppSettings.Settings[mainWindow.PkgPathKey].Value.ToString();
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

        private void Export_Clicked(object sender, RoutedEventArgs e)
        {
            if (mainWindow.ExportSettings.Hash == "")
            {
                System.Windows.MessageBox.Show("No dynamic model selected");
                return;
            }
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.Filter = "Model files | *.fbx";
                dialog.DefaultExt = "fbx";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                mainWindow.ExportSettings.Path = dialog.FileName;
            }
            bool status = mainWindow.ExportSettings.Export(GetPackagesPath());
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

            if (CurrentPkg.Dynamics.Count == 0)
            {
                System.Windows.MessageBox.Show("No PKG selected");
                return;
            }

            string OutputPath;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                OutputPath = dialog.SelectedPath;
            }
            if (OutputPath == "")
            {
                System.Windows.MessageBox.Show("No output path selected");
                return;
            }

            bool status = true;
            foreach (Dynamic dynamic in CurrentPkg.Dynamics)
            {
                mainWindow.ExportSettings.Hash = dynamic.HashString;
                mainWindow.ExportSettings.Path = $"{OutputPath}/{CurrentPkg}/{dynamic.Hash.ToString()}.fbx";
                status &= mainWindow.ExportSettings.Export(GetPackagesPath());
            }
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
