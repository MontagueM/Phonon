using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace Phonon
{
    /// <summary>
    /// Interaction logic for MapView.xaml
    /// </summary>
    public partial class MapView : UserControl
    {
        Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> MapInfoDict;
        string PkgName = "";
        private MainWindow mainWindow = null; // Reference to the MainWindow
        public MapView()
        {
            InitializeComponent();
            ReadMapListData();
        }
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Window.GetWindow(this) as MainWindow;
            ShowPkgList();
        }

        private void ReadMapListData()
        {
            MapInfoDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>>(File.ReadAllText("D1MapInfo.json"));
        }

        private void ShowMapList(string PkgName)
        {
            this.PkgName = PkgName;
            var Info = MapInfoDict[PkgName];

            MapsList.Children.Clear();

            List<string> MapNames = Info.Keys.ToList<string>();
            MapNames.Sort();
            Style style = Application.Current.Resources["Button_Command"] as Style;
            foreach (string MapName in MapNames)
            {
                // We want to verify that this pkg has at least 1 dynamic model in it.
                ToggleButton btn = new ToggleButton();
                btn.Focusable = true;
                btn.Focus();
                btn.Content = new TextBlock
                {
                    Text = MapName,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                };
                btn.Style = style;
                btn.Height = 50;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                MapsList.Children.Add(btn);
            }
            ScrollView2.ScrollToTop();
        }

        private void PkgButton_Click(object sender, RoutedEventArgs e)
        {
            string ClickedPackageName = (((sender as ToggleButton).Content) as TextBlock).Text;
            foreach (ToggleButton button in PrimaryList.Children)
            {
                button.IsChecked = false;
            }
            (sender as ToggleButton).IsChecked = true;
            ShowMapList(ClickedPackageName);
        }
        public string GetPackagesPath()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);
            if (config.AppSettings.Settings["PackagesPathD1"] == null)
            {
                System.Windows.MessageBox.Show($"No package path found for Destiny 1.");
                return "";
            }
            return config.AppSettings.Settings["PackagesPathD1"].Value.ToString();
        }
        private void ExtractSelectedMapsButton_Click(object sender, RoutedEventArgs e)
        {
            // Get static hashes
            List<string> MapNames = new List<string>();
            foreach (ToggleButton button in MapsList.Children)
            {
                if (button.IsChecked == true)
                {
                    MapNames.Add(((button.Content) as TextBlock).Text);
                }
                button.IsChecked = false;
            }

            if (MapNames.Count == 0)
            {
                System.Windows.MessageBox.Show("No maps selected");
                return;
            }
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (dialog.SelectedPath == "")
                {
                    return;
                }
                mainWindow.ExportSettings.Path = dialog.SelectedPath;
            }
            string PkgsPath = GetPackagesPath();
            if (PkgsPath == "")
            {
                return;
            }
            bool status = mainWindow.ExportSettings.ExportD1Map(PkgsPath, MapNames, MapInfoDict[PkgName]);
            if (status)
            {
                System.Windows.MessageBox.Show("Export success");
            }
            else
            {
                System.Windows.MessageBox.Show("Export failed");
            }
        }

        private void ShowPkgList()
        {
            MapsList.Children.Clear();
            PrimaryList.Children.Clear();

            List<string> PackageNames = MapInfoDict.Keys.ToList<string>();
            PackageNames.Sort();
            Style style = Application.Current.Resources["Button_Command"] as Style;

            foreach (string PkgName in PackageNames)
            {
                // We want to verify that this pkg has at least 1 dynamic model in it.
                ToggleButton btn = new ToggleButton();
                btn.Focusable = true;
                btn.Focus();
                btn.Content = new TextBlock
                {
                    Text = PkgName,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                }; ;
                btn.Style = style;
                btn.Height = 50;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
                btn.Click += PkgButton_Click;
                PrimaryList.Children.Add(btn);
            }
            ScrollView.ScrollToTop();
        }
    }
}
