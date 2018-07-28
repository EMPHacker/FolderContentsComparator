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

            public Dictionary<string, bool> Files;
            public Dictionary<string, FolderStructure> Subdirectories;
            
            public FolderStructure()
            {
                Duplicate = false;
                Files = new Dictionary<string, bool>();
                Subdirectories = new Dictionary<string, FolderStructure>();
            }
        }
        public class FileItem
        {
            public string FileName;
            public bool Duplicate;            
        }

        FolderStructure Folder1;
        FolderStructure Folder2;
        TreeViewItem RootNode1;
        TreeViewItem RootNode2;

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
                if ((RootNode1 is null) && (RootNode2 is null))
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
                        RootNode1 = new TreeViewItem
                        {
                            Header = Folder1.Name,
                            FontWeight = FontWeights.Normal
                        };
                        RootNode2 = new TreeViewItem
                        {
                            Header = Folder2.Name,
                            FontWeight = FontWeights.Normal
                        };

                        Task CompGen = Task.Factory.StartNew(() =>
                        {
                            CompareFolders(Folder1, Folder2);
                            CompareFolders(Folder2, Folder1);
                        });
                        CompGen.Wait();

                        if (CompGen.IsCompleted)
                        {
                            GenerateTreeNodes(Folder1, RootNode1);
                            GenerateTreeNodes(Folder2, RootNode2);

                            ContentsFolder1.Items.Add(RootNode1);
                            ContentsFolder2.Items.Add(RootNode2);

                            VisualFeedbackBar.Visibility = Visibility.Hidden;
                        }
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
                string scrubbedname = filename.Split('\\').Last();
                thisDirectory.Files.Add(scrubbedname, false);
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
                thisDirectory.Subdirectories.Add(NewSubfolder.Name, NewSubfolder);

                ProcessSubdirectory(NewSubfolder, subdir);
            }
        }
        
        private void CompareFolders(FolderStructure firstFolder, FolderStructure secondFolder)
        {
            foreach (KeyValuePair<string, bool> file in firstFolder.Files)
            {
                if (secondFolder.Files.ContainsKey(file.Key))
                {
                    secondFolder.Files[file.Key] = true;
                }
            }

            foreach (KeyValuePair<string, FolderStructure> subdir in firstFolder.Subdirectories)
            {
                secondFolder.Subdirectories.TryGetValue(subdir.Key, out FolderStructure subdiredit);

                if (subdiredit != null)
                {
                    subdiredit.Duplicate = true;                    
                    CompareFolders(subdir.Value, subdiredit);
                    secondFolder.Subdirectories[subdir.Key] = subdiredit;
                }
            }
        }

        private void GenerateTreeNodes(FolderStructure Folder, TreeViewItem rootNode)
        {
            Folder.Files.OrderBy(i => i.Key);
            foreach (KeyValuePair<string, bool> file in Folder.Files)
            {
                TreeViewItem newnode = new TreeViewItem
                {
                    Header = file.Key,
                    FontWeight = FontWeights.Normal
                };
                if (file.Value)
                {
                    newnode.Foreground = new SolidColorBrush(Colors.Red);
                }
                rootNode.Items.Add(newnode);
            }
            Folder.Subdirectories.OrderBy(i => i.Key);
            foreach (KeyValuePair<string, FolderStructure> subdir in Folder.Subdirectories)
            {                
                TreeViewItem newnode = new TreeViewItem
                {
                    Header = subdir.Key,
                    FontWeight = FontWeights.Normal
                };
                if (subdir.Value.Duplicate)
                {
                    newnode.Foreground = new SolidColorBrush(Colors.Red);
                }
                rootNode.Items.Add(newnode);
                GenerateTreeNodes(subdir.Value, newnode);
            }
        }
        
        private void ContentsFolder1_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi)
            {
                ExpandTargetNode(RootNode2, tvi.Header);
            }
        }
        private void ContentsFolder2_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi)
            {
                ExpandTargetNode(RootNode1, tvi.Header);
            }
        }
        /*
        void FindTargetNode(TreeViewItem tvi, object NodeName)
        {
            if (tvi.Header.ToString() == NodeName.ToString())
            {
                tvi.IsSelected = true;
                tvi.BringIntoView();
                return;
            }
            //else { tvi.IsExpanded = false; }
            if (tvi.HasItems)
            {
                foreach (var item in tvi.Items)
                {
                    TreeViewItem temp = item as TreeViewItem;
                    ExpandTargetNode(temp, NodeName);
                }
            }
        }*/
        void ExpandTargetNode(TreeViewItem tvi, object NodeName)
        {
            if (tvi.Header.ToString() == NodeName.ToString())
            {
                tvi.IsExpanded = true;
                tvi.BringIntoView();
                return;
            }
            //else { tvi.IsExpanded = false; }
            if (tvi.HasItems)
            {
                foreach (var item in tvi.Items)
                {
                    TreeViewItem temp = item as TreeViewItem;
                    ExpandTargetNode(temp, NodeName);
                }
            }
        }
        void CollapseTargetNode(TreeViewItem tvi, object NodeName)
        {
            if (tvi.Header.ToString() == NodeName.ToString())
            {
                tvi.IsExpanded = false;
                tvi.BringIntoView();
                return;
            }
            if (tvi.HasItems)
            {
                foreach (var item in tvi.Items)
                {
                    TreeViewItem temp = item as TreeViewItem;
                    CollapseTargetNode(temp, NodeName);
                }
            }
        }

        private void ContentsFolder1_Collapsed(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi)
            {
                CollapseTargetNode(RootNode2, tvi.Header);
            }
        }
        private void ContentsFolder2_Collapsed(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi)
            {
                CollapseTargetNode(RootNode1, tvi.Header);
            }
        }

        private void ContentsFolder1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            /*
            if (e.OriginalSource is TreeViewItem tvi)
            {
                FindTargetNode(RootNode2, tvi.Header);
            }*/
        }
    }
}
