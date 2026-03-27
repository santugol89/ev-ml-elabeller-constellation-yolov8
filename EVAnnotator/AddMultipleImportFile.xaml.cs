using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for AddMultipleImportFile.xaml
    /// </summary>
    public partial class AddMultipleImportFile : Window, INotifyPropertyChanged
    {
        MainWindow app;
        String fileType;
        public event PropertyChangedEventHandler PropertyChanged;
        public bool bDuplicateFound;
        Thread threadDataSheetImport;
        BackgroundWorker BGWorkerAddFile;
        bool bIsPredictedData = false;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        ObservableCollection<string> csvFileList = new ObservableCollection<string>();
        public ObservableCollection<string> CSVFileList
        {
            get
            {
                return csvFileList;
            }
            set
            {
                csvFileList = value;
                NotifyPropertyChanged("CSVFileList");
            }
        }

        public AddMultipleImportFile(MainWindow app, String FileType)
        {
            InitializeComponent();
            this.app = app;
            this.fileType = FileType;
            InitializeControls();
            DataContext = this;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ButtonClose_Click(null,null);
        }

        private void InitializeControls()
        {
            if (fileType == "CSV")
            {
                lblHeading.Content = "Add Multiple CSVs";
                btnAddCSV.Content = "Add CSV";
                btnAddCSV.ToolTip = "Add csv files";
            }
            else if(fileType == "JSON" || fileType == "WorkCell")
            {
                lblHeading.Content = "Add Multiple JSONs";
                btnAddCSV.Content = "Add JSON";
                btnAddCSV.ToolTip = "Add json files";
            }
            else if (fileType == "XML")
            {
                lblHeading.Content = "Add XML";
                btnAddCSV.Content = "Add XML";
                btnAddCSV.ToolTip = "Add XML Folder";
            }
        }

        private void btnAddCsv_Click(object sender, RoutedEventArgs e)
        {
            if(fileType == "CSV" || fileType == "JSON" || fileType == "WorkCell")
            {
                OpenFileDialog openFileDiag = new OpenFileDialog();
                openFileDiag.InitialDirectory = app.settings.LoadCSVImportPath;
                openFileDiag.Filter = fileType == "CSV" ? "csv file|*.csv" : "json file|*.json";
                openFileDiag.Multiselect = true;
                DialogResult result = openFileDiag.ShowDialog();
                bDuplicateFound = false;

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    CheckForDuplicateFiles(openFileDiag.FileNames);                    
                    //result = openFileDiag.ShowDialog();
                }
            }
            else if(fileType == "XML")
            {
                FolderBrowserDialog openFolderDiag = new FolderBrowserDialog();
                openFolderDiag.SelectedPath = app.settings.LoadCSVImportPath;
                bDuplicateFound = false;
                DialogResult result = openFolderDiag.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    bDuplicateFound = false;
                    if (!CSVFileList.Contains(openFolderDiag.SelectedPath)){
                        string[] filesInCurrent = Directory.GetFiles(openFolderDiag.SelectedPath, "*.xml", SearchOption.TopDirectoryOnly);
                        if(filesInCurrent.Length > 0){
                            app.settings.LoadCSVImportPath = System.IO.Path.GetDirectoryName(openFolderDiag.SelectedPath);
                            CSVFileList.Add(openFolderDiag.SelectedPath);
                        }
                        else{
                            System.Windows.MessageBox.Show("No XML files found in selected folder.!\nPlease Select folder which has XML files..!", "No Files", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                        bDuplicateFound = true;

                    if (bDuplicateFound)
                        System.Windows.MessageBox.Show("Duplicate files cannot be added..!", "File Exist", MessageBoxButton.OK);
                }
            }
        }

        private void CheckForDuplicateFiles(string[] SelectedFiles)
        {
            bDuplicateFound = false;
            if (app.settings.ApplicationMode == "Test")
            {
                foreach (string filename in SelectedFiles)
                {
                    if(app.settings.ImportFilePath != null && app.settings.ImportFilePath.Contains(filename))
                    {
                        System.Windows.MessageBox.Show("Some of files have been already Added..! ", "File Exist", MessageBoxButton.OK,MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            foreach (string filename in SelectedFiles)
            {
                if (!CSVFileList.Contains(filename))
                {
                    app.settings.LoadCSVImportPath = System.IO.Path.GetDirectoryName(filename);
                    CSVFileList.Add(filename);
                }
                else
                    bDuplicateFound = true;
            }
            if (bDuplicateFound)
            {
                System.Windows.MessageBox.Show("Duplicate files cannot be added..!", "File Exist", MessageBoxButton.OK);
                return;
            }
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            if (CSVFileList.Count == 0)  {
                System.Windows.MessageBox.Show("Please add datasheet..!", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (app.settings.classCount == 0 && fileType != "WorkCell")
            {
                System.Windows.MessageBox.Show("Please select Class from \"File->Settings\" menu before import..!", "Class No found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            for (int i = 0; i < CSVFileList.Count; i++){
                if (app.settings.CheckFileAccess(CSVFileList[i])){
                    System.Windows.MessageBox.Show("Some file cannot be accessible\nMake sure the file is not accessed by other application.", "Access Denied", MessageBoxButton.OK, 
                        MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);

                    CSVFileList.Clear();
                    return;
                }
            }

            if(app.settings.ApplicationMode == "Test")
                popUpWait.IsOpen = true;
            else
                ProcessImportFiles();
        }

        private void ProcessImportFiles()
        {
            //app.settings.LoadCSVImportPath = System.IO.Path.GetDirectoryName(CSVFileList[CSVFileList.Count - 1]);
            app.settings.DefaultImageLoadPath = Regex.Split(app.settings.LoadCSVImportPath, "Output").First() != "" ? Regex.Split(app.settings.LoadCSVImportPath, "Output").First() : app.settings.LoadCSVImportPath;
            app.settings.ImportFilePath = CSVFileList.ToArray();

            BGWorkerAddFile = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            BGWorkerAddFile.DoWork += bgwDowork_LoadDatasheet;
            BGWorkerAddFile.ProgressChanged += app.bgwProgressChange_Load;

            BGWorkerAddFile.RunWorkerAsync();
            app.OnWorkerMethodStartWithPercent_ProcessFile(this);
        }

        private void bgwDowork_LoadDatasheet(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerAddFile.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {               
                threadDataSheetImport = new Thread(LoadImportDataSheetFiles);
                threadDataSheetImport.IsBackground = true;
                threadDataSheetImport.Start();              
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            popUpWait.IsOpen = false;
            app.OnWorkerMethodComplete("complete");
        }

        private void LoadImportDataSheetFiles()
        {
            bool blnStatus = true;
            try
            {
                app.labelEvent.Reset();
                app.SaveEvent.Reset();
                app.CleanupLoadedData();
                app.settings.nImportFileRecordCount = new int[app.settings.ImportFilePath.Length];

                if (fileType == "CSV"){
                    blnStatus = app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline ?
                                app.LoadProcessedImageFromCSV(bIsPredictedData) : app.LoadSegregatedImagesFromCSV();
                   
                    if (blnStatus && app.settings.blnValidationStat && app.settings.ApplicationMode == "Normal")
                    {
                        app.LoadViolatedDataFromCSV();
                    }
                }
                else if (fileType == "JSON"){                    
                    blnStatus = app.LoadProcessedImageFromJSON();
                }

                else if(fileType == "XML"){
                    blnStatus = app.LoadProcessedImageFromXML();
                }

                else if (fileType == "WorkCell")
                {
                    blnStatus = app.LoadProcessedImageFromWorkCellJSON();
                }

                if (blnStatus){
                    app.ImageClassMatching();
                    foreach (ClassFolderStat curClassStat in app.ListClassFolderStat){
                        var curItem = app.ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == curClassStat.ClassAliasName.ToUpper());
                        if (curItem != null)
                            curItem.ModifiedID = curClassStat.ClassID;
                        else
                            app.ListModifiedClass.Add(new ModifiedClass(curClassStat.ClassID, curClassStat.ClassAliasName));
                    }
                    if (fileType == "CSV" && app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline) { 
                        app.UpdateAugmentationClassList();
                    }
                        
                    this.Dispatcher.Invoke(() => {                        
                        for(int i = 0; i < app.settings.ImportFilePath.Length; i++)
                        {
                            System.IO.File.SetAttributes(app.settings.ImportFilePath[i], System.IO.FileAttributes.ReadOnly);
                            Utilities.LogMessage(app.settings.ImportFilePath[i] + "Import CSV Completed", 0);
                        }

                        if (!app.bWorkCellMode) {
                            app.InitializeComboBox();
                            app.IsVisibleMultiCSVExport = app.ListDatasheetImportData.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                        }

                        if (fileType == "CSV")
                        {
                            app.LoadClasswiseTimeAnalysisGraph();
                        }
                        app.OnWorkerMethodComplete("complete");
                        if (app.ImageMenuList.Count > 0)
                        {
                            //app.ListBoxImages_SelectionChanged(null, null);
                            app.cmbSort.SelectedIndex = 0;
                            app.cmbClassFilter.SelectionChanged -= app.cmbClassFilter_SelectionChanged;
                            app.cmbClassFilter.SelectedIndex = 0;
                            app.cmbClassFilter.SelectionChanged += app.cmbClassFilter_SelectionChanged;
                            //app.listBoxImages.SelectedIndex = 0;
                        }
                        System.Windows.MessageBox.Show("Selected files loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, 
                            MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                        
                        app.labelEvent.Set();
                        app.SaveEvent.Set();
                        app.bIsLoadLabellingGraph = true;
                        this.Close();
                    });
                }
                else{
                    this.Dispatcher.Invoke(() =>
                    {
                        app.labelEvent.Set();
                        app.SaveEvent.Set();
                        app.OnWorkerMethodComplete("complete");
                        app.CleanupLoadedData();
                        app.settings.ImportFilePath = null;
                        app.settings.nImportFileRecordCount = null;
                        CSVFileList.Clear();
                        if (app.ImageMenuList.Count > 0)
                            app.ListBoxImages_SelectionChanged(null, null);
                    });
                }                            
            }

            catch (System.Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                app.CleanupLoadedData();
                app.settings.ImportFilePath = null;
                app.settings.nImportFileRecordCount = null;
                app.labelEvent.Set();
                app.SaveEvent.Set();
                Utilities.LogMessage("AddMultipleImportFile::ImportMultipleCSV_Click: " + ex.Message, 9);
                if (ex.Message != "Thread was being aborted.")
                {
                    System.Windows.MessageBox.Show("Something went wrong..! Loading Failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    this.Dispatcher.Invoke((() =>
                    {
                        if (threadDataSheetImport != null && threadDataSheetImport.IsAlive)
                            threadDataSheetImport.Abort();
                        this.Close();
                    }));
                }
            }
        }

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            if (threadDataSheetImport != null && threadDataSheetImport.IsAlive)
                threadDataSheetImport.Abort();
            this.Close();
        }

        private void menuClear_Click(object sender, MouseButtonEventArgs e)
        {
            if (CSVFileList.Count > 0)
                CSVFileList.Clear();
        }

        private void menuClear_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (CSVFileList.Count > 0)
                menuClear.IsEnabled = true;
            else
                menuClear.IsEnabled = false;
        }

        private void btnModeOk_Click(object sender, RoutedEventArgs e)
        {
            popUpWait.IsOpen = false;
            bIsPredictedData = radPredictData.IsChecked.Value;
            ProcessImportFiles();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            popUpWait.IsOpen = false;
        }
    }
}
