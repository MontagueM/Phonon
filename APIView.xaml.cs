using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Phonon
{
    /// <summary>
    /// Interaction logic for APIView.xaml
    /// </summary>
    public partial class APIView : UserControl
    {
        Dictionary<string, Item> ItemsDict = new Dictionary<string, Item>();
        private MainWindow mainWindow = null;

        public APIView()
        {
            InitializeComponent();
            CacheAllItems();
            RefreshItemList();
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Window.GetWindow(this) as MainWindow;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshItemList();
        }

        private void RefreshItemList()
        {
            List<Item> items = new List<Item>();
            string SearchStr = textBox.Text.ToLower();
            if (SearchStr != "")
            {
                // Select and sort by relevance to selected string
                Parallel.ForEach(ItemsDict.Keys, Name =>
                {
                    if (items.Count > 30) return;
                    if (Name.Contains(SearchStr))
                    {
                        items.Add(ItemsDict[Name]);
                    }
                });
            }
            ListViewAPI.ItemsSource = items;
        }
        private void ExtractGearBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (dialog.SelectedPath == "")
                {
                    return;
                }
                mainWindow.ExportSettings.Path = dialog.SelectedPath;
            }
            mainWindow.ExportSettings.SaveName = ((sender as Button).DataContext as Item).Name;
            List<string> Models = ((sender as Button).DataContext as Item).Models;
            //Parallel.ForEach(Models, ModelHash =>
            foreach (string ModelHash in Models)
            {
                mainWindow.ExportSettings.Hash = ModelHash;
                mainWindow.ExportSettings.Export(GetPackagesPath());
            }//);
            System.Windows.MessageBox.Show("Export success");
            var a = 0;
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
        private void CacheAllItems()
        {
            ItemsDict = JsonConvert.DeserializeObject<Dictionary<string, Item>>(File.ReadAllText("APIGear.json"));
        }
    }

    public class Item
    {
        public uint Hash { get; set; }
        public int Index { get; set; }
        public List<string> Models { get; set; }
        public string Name { get; set; }
    }
}
