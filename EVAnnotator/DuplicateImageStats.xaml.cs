using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Delimon;
using Alphaleonis;
using System.Windows.Data;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for DuplicateImageStats.xaml
    /// </summary>
    public partial class DuplicateImageStats : Window, INotifyPropertyChanged
    {
        MainWindow app;
        public List<ImageFolder> listImageFolder = new List<ImageFolder>();
        public List<DuplicateImage> listDuplicateImages = new List<DuplicateImage>();
        public event PropertyChangedEventHandler PropertyChanged;
        BackgroundWorker BGWorkerFormat;

        public DuplicateImageStats(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            InitializeControls();
            this.DataContext = this;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void InitializeControls()
        {
            lblParentFolder.Text = "";
            lblDupImage.Text = "0"; 

        }

        public double GetColumnWidth
        {
            get
            {
                return this.Width - 250;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (app.ImageMenuList == null || app.ImageMenuList.Count == 0 || app.settings.LoadedImagefiles == null)
            {
                lblParentFolder.Text = "No Image folder loaded";
                return;
            }

            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            BGWorkerFormat.DoWork += bgwDowork_DuplicateStat;
            BGWorkerFormat.ProgressChanged += app.bgwProgressChange_Load;
            BGWorkerFormat.RunWorkerAsync();
            app.OnWorkerMethodStart_LoadFile(app);
        }

        private void bgwDowork_DuplicateStat(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadProcess = new Thread(GetImagesFromParentFolder);
                threadProcess.IsBackground = true;
                threadProcess.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            app.OnWorkerMethodComplete("complete");
        }

        private string duplicateCount = "0";
        public string DuplicateCount
        {
            get {
                return duplicateCount;
            }
            set {
                duplicateCount = value;
                NotifyPropertyChanged("DuplicateCount");
            }
        }

        private void GetImagesFromParentFolder()
        {
            List<string> ListLoadedImages = app.GetAllFilesFromDirectory(app.settings.LoadImagePath);
            foreach (string imageFile in ListLoadedImages)
            {
                ImageFolder curImageFolder = listImageFolder.FirstOrDefault(temp => temp.ImageFolderName == Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(imageFile));
                if (curImageFolder == null)
                {
                    listImageFolder.Add(new ImageFolder
                    {
                        ImageFolderName = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(imageFile),
                        ImageCount = 1,
                        DuplicateImageList = new List<DuplicateImage>()
                    });
                }
                else
                    curImageFolder.ImageCount++;
            }

            var listDistinctImages = ListLoadedImages.DistinctBy(item => Alphaleonis.Win32.Filesystem.Path.GetFileName(item)).ToList();
            var listTempDuplicates = ListLoadedImages.Except(listDistinctImages).Select(temp => Alphaleonis.Win32.Filesystem.Path.GetFileName(temp)).ToList();

            foreach (string dupImage in listTempDuplicates)
            {
                DuplicateImage curDupImage = listDuplicateImages.FirstOrDefault(item => item.DuplicateImageName == dupImage);
                if (curDupImage != null)
                    continue;

                var listTempImages = ListLoadedImages.Where(item => Alphaleonis.Win32.Filesystem.Path.GetFileName(item) == dupImage)
                                    .Select(temp => Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(temp)).ToList();
                listDuplicateImages.Add(new DuplicateImage
                {
                    DuplicateImageName = dupImage,
                    DuplicateFolderList = listTempImages
                });
            }

            //int nDupCount = 0;
            //for (int i = 0; i < listImageFolder.Count; i++)
            //    nDupCount += listImageFolder[i].DuplicateImageCount;
            this.Dispatcher.Invoke((() => {
                lvFolderList.ItemsSource = listImageFolder;
                listDupImageList.ItemsSource = listDuplicateImages;
                lblParentFolder.Text = app.settings.LoadImagePath;
                lblParentFolder.ToolTip = app.settings.LoadImagePath;
                //DuplicateCount = nDupCount.ToString();
                DuplicateCount = listTempDuplicates.Count.ToString();
                if (listDuplicateImages.Count > 0)
                    menuCopy.IsEnabled = true;
                else
                    menuCopy.IsEnabled = false;
            }));
            Utilities.LogMessage("Images loaded from parent folder.");
            app.OnWorkerMethodComplete("complete");
        }
        
        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void listDupImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listDupImageList.SelectedItem == null)
                return;

            DuplicateImage curDupImage = listDupImageList.SelectedItem as DuplicateImage;
            listDupFolder.ItemsSource = curDupImage.DuplicateFolderList;
        }

        private void ButtonMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext.ToString() != "")
            {
                listDupFolder.SelectedItem = (sender as Button).DataContext.ToString();
                DuplicateImage curDupImage = listDupImageList.SelectedItem as DuplicateImage;
                //string filePath = Path.Combine((sender as Button).DataContext.ToString(), curDupImage.DuplicateImageName);
                //Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));

                OpenFolderAndSelectItem((sender as Button).DataContext.ToString(), curDupImage.DuplicateImageName);
            }
        }

        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            if (listDupImageList.SelectedItem == null)
                return;
            DuplicateImage curDupImage = listDupImageList.SelectedItem as DuplicateImage;
            Clipboard.SetText(curDupImage.DuplicateImageName);            
        }

        private void btnDeleteDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext as DuplicateImage != null)
            {
                DuplicateImage curDupImage = (sender as Button).DataContext as DuplicateImage;
                listDupImageList.SelectedItem = curDupImage;
                MessageBoxResult result = MessageBox.Show("Do you want to delete duplicate image?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes)
                {
                    DeleteDuplicateImage(curDupImage);
                }
            }
        }

        private void DeleteDuplicateImage(DuplicateImage curDupImage)
        {
            for (int i = 1; i < curDupImage.DuplicateFolderList.Count; i++)
            {
                string filePath = Path.Combine(curDupImage.DuplicateFolderList[i].ToString(), curDupImage.DuplicateImageName);
                //if (File.Exists(filePath))
                //    File.Delete(filePath);

                if (Alphaleonis.Win32.Filesystem.File.Exists(filePath))
                    Alphaleonis.Win32.Filesystem.File.Delete(filePath);

                ImageFolder curImageFolder = listImageFolder.Where(item => item.ImageFolderName == curDupImage.DuplicateFolderList[i].ToString()).FirstOrDefault();
                if (curImageFolder != null)
                {
                    curImageFolder.ImageCount--;
                    curImageFolder.DuplicateImageList.Remove(curDupImage);
                }
            }
            listDuplicateImages.Remove(curDupImage);
            listDupImageList.Items.Refresh();
            lvFolderList.Items.Refresh();
            listDupFolder.ItemsSource = null;
            int nDupCount = listDuplicateImages.Sum(item => item.DuplicateFolderList.Count - 1);
            DuplicateCount = nDupCount.ToString();
            app.TotalDuplicateImages = nDupCount;
            app.TotalImagesPresent = app.TotalImagesLoaded + nDupCount;
        }

        private void btnDeleteAllDup_Click(object sender, RoutedEventArgs e)
        {
            if(listDuplicateImages.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to delete all duplicate images?", "Delete All", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    for (int cnt = 0; cnt < listDuplicateImages.Count; cnt++)
                    {
                        DuplicateImage curDupImage = listDuplicateImages[cnt] as DuplicateImage;
                        DeleteDuplicateImage(curDupImage);
                        cnt--;
                    }
                    Utilities.LogMessage("All Duplicate images deleted.");
                }
            }
            else
                MessageBox.Show("No Duplicate images present", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ButtonCSVExport_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (app.ImageMenuList.Count == 0)
                {
                    MessageBox.Show("No Image Folder Loaded..!", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Duplicate Image Stats";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");

                string seperator = ",";
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Join(seperator, "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt")));
                sb.AppendLine(string.Join(seperator, "Parent Folder : " + lblParentFolder.Text));
                sb.AppendLine(string.Join(seperator, "Duplicate Images : " + DuplicateCount));
                sb.AppendLine("Folder Name,Images Present");

                for (int i = 0; i < listImageFolder.Count; i++)
                {
                    ImageFolder curImageFolder = listImageFolder[i] as ImageFolder;
                    sb.AppendLine(string.Join(seperator, curImageFolder.ImageFolderName, curImageFolder.ImageCount));
                }

                File.WriteAllText(strCSVSavePath, sb.ToString());
                MessageBox.Show("CSV Exported Successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, 
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("The specified Output Data path, file name, or both are too long.");
            }
        }

 
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        public static void OpenFolderAndSelectItem(string folderPath, string file)
        {
            IntPtr nativeFolder;
            uint psfgaoOut;
            SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

            if (nativeFolder == IntPtr.Zero)
            {
                // Log error, can't find folder
                return;
            }

            IntPtr nativeFile;
            SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

            IntPtr[] fileArray;
            if (nativeFile == IntPtr.Zero)
            {
                // Open the folder without the file selected if we can't find the file
                fileArray = new IntPtr[0];
            }
            else
            {
                fileArray = new IntPtr[] { nativeFile };
            }

            SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

            Marshal.FreeCoTaskMem(nativeFolder);
            if (nativeFile != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeFile);
            }
        }

        private void TabControl_SelectionChanged(object sender, Telerik.Windows.Controls.RadSelectionChangedEventArgs e)
        {
            if (tabDuplicateStat.SelectedIndex == 0)
                btnCSVExport.Visibility = Visibility.Visible;
            else
                btnCSVExport.Visibility = Visibility.Collapsed;
        }
    }

    public class ImageFolder
    {
        public string ImageFolderName { get; set; }

        public int ImageCount { get; set; }

        public int DuplicateImageCount
        {
            get
            {
                return DuplicateImageList.Count;
            }
        }
        public List<DuplicateImage> DuplicateImageList { get ; set; }
    }

    public class DuplicateImage
    {
        public string DuplicateImageName { get; set; }

        public List<string> DuplicateFolderList { get; set; }       
    }
}
