using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FileCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class FolderStructure
        {
            public string Name;
            public string FolderPath;
            public bool Duplicate;
            public List<FileItem> Files;
            public List<FolderStructure> Subdirectories;

            
            public FolderStructure()
            {
                Duplicate = false;
                Files = new List<FileItem>();
                Subdirectories = new List<FolderStructure>();
            }
        }
        public class FileItem
        {
            public string FileName;
            public bool Duplicate;            
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

        private void CompareProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((URLFolder1.Text != "") && (URLFolder1.Text != ""))
            {
                VisualFeedbackBar.Visibility = Visibility.Visible;

                Task GetFiles = Task.Factory.StartNew(() =>
                {
                    ProcessSubdirectory(Folder1, Folder1.FolderPath);
                    ProcessSubdirectory(Folder2, Folder2.FolderPath);
                });

                GetFiles.Wait();

                if (GetFiles.IsCompleted)
                {
                    TreeViewItem RootNode1 = new TreeViewItem
                    {
                        Header = Folder1.Name,
                        FontWeight = FontWeights.Normal
                    };
                    TreeViewItem RootNode2 = new TreeViewItem
                    {
                        Header = Folder2.Name,
                        FontWeight = FontWeights.Normal
                    };

                    Task SortFiles = Task.Factory.StartNew(() =>
                    {
                        SortFolders(Folder1);
                        SortFolders(Folder2);
                    });
                    SortFiles.Wait();

                    if (SortFiles.IsCompleted)
                    {
                        CompareFolders(Folder1, Folder2);

                        GenerateTreeNodes(Folder1, RootNode1);
                        GenerateTreeNodes(Folder2, RootNode2);

                        ContentsFolder1.Items.Add(RootNode1);
                        ContentsFolder2.Items.Add(RootNode2);

                        VisualFeedbackBar.Visibility = Visibility.Hidden;
                    }
                }
            }
            else
            {
                MessageBox.Show(this, "Select two folders");
            }
        }

        private static void ProcessSubdirectory(FolderStructure thisDirectory, string directoryPath)
        {
            // Process the list of files found in the directory.
            string[] fileslist = Directory.GetFiles(directoryPath);
            foreach (string filename in fileslist)
            {
                FileItem newfile = new FileItem
                {
                    FileName = filename.Split('\\').Last(),
                    Duplicate = false
                };
                thisDirectory.Files.Add(newfile);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirslist = Directory.GetDirectories(directoryPath);
            foreach (string subdir in subdirslist)
            {
                FolderStructure NewSubfolder = new FolderStructure
                {
                    Name = subdir.Split('\\').Last(),
                    FolderPath = subdir
                };
                thisDirectory.Subdirectories.Add(NewSubfolder);

                ProcessSubdirectory(NewSubfolder, subdir);
            }
        }

        public void SortFolders(FolderStructure folder)
        {
            //folder.Files.Sort(x => x.FileName); var sortedEnumerable = 
            folder.Files.OrderBy(p => p.FileName);

            foreach (FolderStructure subdir in folder.Subdirectories)
            {
                SortFolders(subdir);
            }
        }

        private void CompareFolders(FolderStructure firstFolder, FolderStructure secondFolder)
        {
            int fffcount = firstFolder.Files.Count();
            int sffcount = secondFolder.Files.Count();

            if ( fffcount == sffcount)
            {
                for (int i = 0; i < fffcount; i++)
                {
                    if (firstFolder.Files[i].FileName == secondFolder.Files[i].FileName)
                    {
                        firstFolder.Files[i].Duplicate = true;
                        secondFolder.Files[i].Duplicate = true;
                    }                    
                }
            }

            int ffsfcount = firstFolder.Subdirectories.Count();
            int sfsfcount = secondFolder.Subdirectories.Count();

            for (int i = 0; i < ffsfcount; i++)
            {
                if (firstFolder.Subdirectories[i].Name == secondFolder.Subdirectories[i].Name)
                {
                    firstFolder.Subdirectories[i].Duplicate = true;
                    secondFolder.Subdirectories[i].Duplicate = true;
                    CompareFolders(firstFolder.Subdirectories[i], secondFolder.Subdirectories[i]);
                }
            }
        }

        private void GenerateTreeNodes(FolderStructure Folder, TreeViewItem rootNode)
        {
            foreach (FileItem file in Folder.Files)
            {
                TreeViewItem newnode = new TreeViewItem
                {
                    Header = file.FileName,
                    FontWeight = FontWeights.Normal
                };
                if (file.Duplicate)
                {
                    newnode.Foreground = new SolidColorBrush(Colors.Red);
                }
                rootNode.Items.Add(newnode);
            }
            foreach (FolderStructure subdir in Folder.Subdirectories)
            {
                TreeViewItem newnode = new TreeViewItem
                {
                    Header = subdir.Name,
                    FontWeight = FontWeights.Normal
                };
                if (subdir.Duplicate)
                {
                    newnode.Foreground = new SolidColorBrush(Colors.Red);
                }
                rootNode.Items.Add(newnode);
                GenerateTreeNodes(subdir, newnode);
            }
        }
    }
}
