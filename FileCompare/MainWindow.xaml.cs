using System;
using System.Collections.Generic;
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
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FileCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public struct FolderStructure
        {
            public string Name;
            public string FolderPath;
            public List<string> Files;
            public List<FolderStructure> Subdirectories;
                
        }

        FolderStructure Folder1;
        FolderStructure Folder2;

        public MainWindow()
        {
            

            InitializeComponent();
        }

        private void BrowseFolder1_Click(object sender, RoutedEventArgs e)
        {

            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                URLFolder1.Text = dialog.FileName;

                Folder1 = new FolderStructure
                {
                    Name = dialog.FileName.Split('\\').Last(),
                    FolderPath = dialog.FileName
                };
            }
        }

        private void BrowseFolder2_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                URLFolder2.Text = dialog.FileName;
                Folder2 = new FolderStructure
                {
                    Name = dialog.FileName.Split('\\').Last(),
                    FolderPath = dialog.FileName
                };
            }
        }


        private static void ProcessSubdir(FolderStructure ParentDir, string targetdir)
        {
            ParentDir.Subdirectories = new List<FolderStructure>();

            // Process the list of files found in the directory.
            //NewSubFolder.Files = Directory.GetFiles(targetdir).ToList();

            List<string> processedfiles = new List<string>();
            string[] fileslist = Directory.GetFiles(targetdir);
            foreach (string filename in fileslist)
            {
                processedfiles.Add(filename.Split('\\').Last());
            }

            FolderStructure NewSubFolder = new FolderStructure
            {
                Name = targetdir.Split('\\').Last(),
                FolderPath = targetdir,
                Files = processedfiles

            };

            // Recurse into subdirectories of this directory.
            string[] subdirslist = Directory.GetDirectories(targetdir);
            foreach (string subdir in subdirslist)
            {
                ProcessSubdir(NewSubFolder, subdir);
            }


            ParentDir.Subdirectories.Add(NewSubFolder);

        }

        private void CompareProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            VisualFeedbackBar.Visibility = Visibility.Visible;

            Task GetFiles = Task.Factory.StartNew(() => ProcessSubdir(Folder1, Folder1.FolderPath));

            //Task.Run(async() => await ProcessSubdir(Folder1, Folder1.FolderPath));
            //await ProcessSubdir(Folder1, Folder1.FolderPath);

            GetFiles.Wait();
            
            //AddColsFolder1(Folder1, 0);
            foreach (string file in Folder1.Subdirectories[0].Files)
            {
                FolderContents2.Items.Add(new ListViewItem { Content = file });
            }

            ContentsFolder1.Items.Add(new ListViewItem());

        }

        private void AddColsFolder1(FolderStructure folder, int counter)
        {
            DataGridTextColumn newcol = new DataGridTextColumn
            {
                Width = 10,
                Binding = new Binding(counter.ToString())
            };
            ContentsFolder1.Columns.Add(newcol);

            foreach (string file in folder.Files)
            {
                ContentsFolder1.Items.Add(new ListViewItem());

            }


            foreach (FolderStructure subdir in folder.Subdirectories)
            {
                AddColsFolder1(subdir, counter++);                    
            }
        }
    }
}
