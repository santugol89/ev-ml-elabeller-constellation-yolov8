using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ManageAndMergeDatasheet.xaml
    /// </summary>
    public partial class ManageAndMergeDatasheet : Window, INotifyPropertyChanged
    {
        MainWindow app;
        public event PropertyChangedEventHandler PropertyChanged;
        string strProjectname;

        public ManageAndMergeDatasheet(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            InitializeControls();
            strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
            DataContext = this;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        ObservableCollection<string> datasheetList = new ObservableCollection<string>();
        public ObservableCollection<string> DatasheetList
        {
            get
            {
                return datasheetList;
            }
            set
            {
                datasheetList = value;
                NotifyPropertyChanged("DatasheetList");
            }
        }

        private bool isEnableMergeButton = false;
        public bool IsEnableMergeButton
        {
            get
            {
                return isEnableMergeButton;
            }
            set
            {
                isEnableMergeButton = value;
                NotifyPropertyChanged("IsEnableMergeButton");
            }
        }

        public bool IsEnableMergeAllButton
        {
            get
            {
                if (DatasheetList.Count > 1)
                    return true;
                else
                    return false;
            }            
        }

        public bool IsEnableDeleteAllButton
        {
            get
            {
                if (DatasheetList.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        private bool isEnableDeleteButton = false;
        public bool IsEnableDeleteButton
        {
            get
            {
                return isEnableDeleteButton;
            }
            set
            {
                isEnableDeleteButton = value;
                NotifyPropertyChanged("IsEnableDeleteButton");
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
            DatasheetList = new ObservableCollection<string>(app.settings.ImportFilePath);
            listLoadedFile.ItemsSource = DatasheetList;
            DatasheetList.CollectionChanged += DatasheetList_CollectionChanged; 
        }

        private void DatasheetList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("IsEnableMergeAllButton");
            NotifyPropertyChanged("IsEnableDeleteAllButton");
        }
        
        private void listLoadedFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatasheetList.Count == 0)
                return;

            IsEnableDeleteButton = listLoadedFile.SelectedItems.Count > 0? true : false;
            IsEnableMergeButton = listLoadedFile.SelectedItems.Count > 1 ? true : false;
        }

        private void listLoadedFile_LostFocus(object sender, RoutedEventArgs e)
        {
            listLoadedFile.SelectedIndex = -1;
            IsEnableMergeButton = false;
            IsEnableDeleteButton = false;
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ButtonDeleteAll_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to Delete All Datasheet?", "Delete All", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
                return;
            DeleteAllDatasheet();
        }

        private void DeleteAllDatasheet()
        {
            DatasheetList.Clear();
            app.CleanupLoadedData(true);
            app.settings.ImportFilePath = null;
            app.ListDataViolation.Clear();
            if (app.ImageMenuList.Count > 0)
                app.ListBoxImages_SelectionChanged(null, null);
            IsEnableDeleteButton = false;
        }

        private void ButtonDelete_Click(object sender, MouseButtonEventArgs e)
        {
            if (listLoadedFile.SelectedItem == null)
                return;

            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to Delete Selected Datasheet?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
                return;

            if (DatasheetList.Count > 1 && listLoadedFile.SelectedItems.Count != DatasheetList.Count){
                for(int i = 0; i < listLoadedFile.SelectedItems.Count; i++)
                    RemoveItemFromMultipleList(listLoadedFile.SelectedItems[i].ToString());
            }                
            else if (listLoadedFile.SelectedItems.Count == DatasheetList.Count)
                DeleteAllDatasheet();
        }

        private void RemoveItemFromMultipleList(string ImportFile)
        {
            lock (app.ProcessedImageBox)
            {
                for (int index = 0; index < app.ProcessedImageBox.Count; index++){
                    ImageListBox curImageBox = app.ProcessedImageBox[index] as ImageListBox;
                    for(int cnt = 0; cnt < curImageBox.ListImageClass.Count; cnt++){
                        ImageClass curImageClass = curImageBox.ListImageClass[cnt] as ImageClass;
                        if (curImageClass.ImportDatasheetName == ImportFile){
                            app.Dispatcher.Invoke(() => curImageBox.ListImageClass.Remove(curImageClass));
                            cnt--;
                        }
                    }

                    if (curImageBox.ListImageClass.Count == 0){
                        app.ProcessedImageBox.Remove(curImageBox);
                        index--;
                    }
                }

                app.ListClassFolderStat.RemoveAll(item => item.ImportDatasheetName == ImportFile);
                app.ListDataViolation.RemoveAll(item => item.ImagePathName == ImportFile);
                app.ListDatasheetImportData.RemoveAll(item => item.DatasheetName == ImportFile);

                app.ListAugmentTypeClass = new List<AugmentTypeClass>();
                List<ClassStats> listClassStats = app.GetClassFolderStatistics();
                foreach (ClassStats curClassStat in listClassStats)
                    app.ListAugmentTypeClass.Add(new AugmentTypeClass(curClassStat));

                if (app.ListAugmentTypeClass.Count > 0)
                    app.IsVisibleAgmentButton = Visibility.Visible;
                else
                    app.IsVisibleAgmentButton = Visibility.Collapsed;

                Dispatcher.Invoke(() => app.ListAugmentationView.ItemsSource = app.ListAugmentTypeClass);
                app.ListAugmentationView.Items.Refresh();
                DatasheetList.Remove(ImportFile);

                System.IO.File.SetAttributes(ImportFile, System.IO.FileAttributes.Normal);
                var nIndex = Array.IndexOf(app.settings.ImportFilePath, ImportFile);                
                app.settings.ImportFilePath = app.settings.ImportFilePath.Where(item => !item.Contains(ImportFile)).ToArray();
                app.IsVisibleMultiCSVExport = app.ListDatasheetImportData.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                app.TotalDataSheet = app.settings.ImportFilePath.Length;
                app.TotalRecordFound = app.TotalRecordFound - app.settings.nImportFileRecordCount[nIndex];
                app.TotalViolationFound = app.ListDataViolation.Count;
                app.settings.nImportFileRecordCount = app.settings.nImportFileRecordCount.Where(item => item != app.settings.nImportFileRecordCount[nIndex]).ToArray();
                if (app.ImageMenuList.Count > 0)
                    app.ListBoxImages_SelectionChanged(null, null);

                string strStatPath = app.settings.StatsFilePath + @"Temp Files";
                string[] tempFiles = null;
                if (System.IO.Directory.Exists(strStatPath))
                    tempFiles = Directory.GetFiles(strStatPath);

                if(tempFiles != null && tempFiles.Length > 0)
                {
                    string fileDelete = tempFiles.FirstOrDefault(item => Delimon.Win32.IO.Path.GetFileName(item) == Delimon.Win32.IO.Path.GetFileName(ImportFile));
                    if (!string.IsNullOrEmpty(fileDelete))
                        File.Delete(fileDelete);
                }
            }
        }

        private void ButtonMergeDatasheet_Click(object sender, MouseButtonEventArgs e)
        {
            string MergeDatasheet = "";
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to Merge Datasheets?", "Merge", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
                return;

            string[] arrSelDatasheet = (sender as Button).Name == "btnMergeDatasheet"? listLoadedFile.SelectedItems.Cast<string>().ToArray() : null;

            if (app.settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".csv"))
                MergeDatasheet = MergeCSVDatasheets(arrSelDatasheet);

            else if (app.settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".json"))
                MergeDatasheet = MergeJSONDatasheets(arrSelDatasheet);

            else
                MergeDatasheet = MergeXMLDatasheets(arrSelDatasheet);

            if (string.IsNullOrEmpty(MergeDatasheet))
                return;

            if(arrSelDatasheet == null)
            {
                app.SetFileAttributeNormal();
                app.settings.ImportFilePath = new string[] { MergeDatasheet };
                app.ProcessedImageBox.ForEach(temp => temp.ListImageClass.ToList().ForEach(item => item.ImportDatasheetName = MergeDatasheet));
                app.ListClassFolderStat.ForEach(temp => temp.ImportDatasheetName = MergeDatasheet);

                ImportDatasheetData curImportDatasheet = new ImportDatasheetData(MergeDatasheet);
                foreach (ImportDatasheetData temp in app.ListDatasheetImportData)
                    curImportDatasheet.ListImportData.AddRange(temp.ListImportData);

                app.ListDatasheetImportData = new List<ImportDatasheetData>();
                app.ListDatasheetImportData.Add(curImportDatasheet);
            }
            else if(arrSelDatasheet.Length > 0)
            {
                foreach(string curDatsheet in arrSelDatasheet)
                    if (File.Exists(curDatsheet))
                        File.SetAttributes(curDatsheet, System.IO.FileAttributes.Normal);

                var tempArr = app.settings.ImportFilePath.ToList();
                tempArr.RemoveAll(temp => arrSelDatasheet.Contains(temp));
                tempArr.Add(MergeDatasheet);
                app.settings.ImportFilePath = tempArr.ToArray();
                app.ProcessedImageBox.ForEach(temp => temp.ListImageClass.Where(sheet => arrSelDatasheet.Contains(sheet.ImportDatasheetName)).ToList().ForEach(item => item.ImportDatasheetName = MergeDatasheet));
                app.ListClassFolderStat.Where(sheet => arrSelDatasheet.Contains(sheet.ImportDatasheetName)).ToList().ForEach(temp => temp.ImportDatasheetName = MergeDatasheet);

                ImportDatasheetData curImportDatasheet = new ImportDatasheetData(MergeDatasheet);
                foreach (ImportDatasheetData temp in app.ListDatasheetImportData)
                    if(arrSelDatasheet.Contains(temp.DatasheetName))
                        curImportDatasheet.ListImportData.AddRange(temp.ListImportData);

                app.ListDatasheetImportData.RemoveAll(temp => arrSelDatasheet.Contains(temp.DatasheetName));
                app.ListDatasheetImportData.Add(curImportDatasheet);
            }

            System.IO.File.SetAttributes(MergeDatasheet, System.IO.FileAttributes.ReadOnly);            
            app.IsVisibleMultiCSVExport = app.ListDatasheetImportData.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            app.TotalDataSheet = app.settings.ImportFilePath.Length; 
            app.TotalViolationFound = 0;
            if (app.settings.blnValidationStat && app.ListDataViolation.Count > 0 && app.settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".csv"))
                app.LoadViolatedDataFromCSV(arrSelDatasheet);
            app.ClearStatTempFiles();
            app.bIsFormatFile = true;
            InitializeControls();
        }

        private string MergeJSONDatasheets(string[] arrSelDatasheet)
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Merge Datasheet\Merge JSON";
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);
                string strJSONSavePath = System.IO.Path.Combine(strDataPath, "json_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".json");

                string[] arrMergeDasheet;
                if (arrSelDatasheet == null)
                    arrMergeDasheet = app.settings.ImportFilePath;
                else
                    arrMergeDasheet = app.settings.ImportFilePath.Where(item => arrSelDatasheet.Contains(item)).ToArray();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("\"project\" : \"" + strProjectname + "\",");

                for (int index = 0; index < arrMergeDasheet.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(arrMergeDasheet[index])))
                    {
                        string jsonLines = reader.ReadToEnd();

                        JObject jsonObj = JsonConvert.DeserializeObject(jsonLines) as JObject;
                        List<object> listObject = (jsonObj as IEnumerable<object>).ToList();
                        if (listObject.Count <= 0)
                            continue;

                        var objectType = (listObject[0] as JProperty).Value.GetType();
                        if (objectType.Name == "JValue")
                            listObject.RemoveAt(0);

                        int count = 0;
                        while (count < listObject.Count)
                        {
                            string json = JsonConvert.SerializeObject(listObject[count], Formatting.Indented);
                            sb.Append(json);
                            count++;
                            if (count < listObject.Count)
                                sb.Append(",");
                        }
                    }
                    if (index < arrMergeDasheet.Length - 1)
                        sb.Append(",");
                }
                sb.Append("}");
                File.WriteAllText(strJSONSavePath, sb.ToString());
                return strJSONSavePath;
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong..!Merge Failed.", "Error!!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ManageAndMergeDatasheet.xaml.cs::MergeCSVDatasheets: " + ex.Message, 0);
            }

            return string.Empty;
        }

        private string MergeCSVDatasheets(string[] arrSelDatasheet)
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Merge Datasheet\Merge CSV";
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);
                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");

                string[] arrColumnHeader;
                List<ImportDatasheetData> tempListDataSheet = new List<ImportDatasheetData>();
                if (arrSelDatasheet == null || arrSelDatasheet.Length == 0) {
                    tempListDataSheet = app.ListDatasheetImportData;
                    arrColumnHeader = app.DictColHeaders.Values.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);

                    app.DictColHeaders.Clear();
                    app.DictColHeaders[strCSVSavePath] = arrColumnHeader;
                }                    
                else{                    
                    for (int i = 0; i < app.ListDatasheetImportData.Count; i++)
                        if (arrSelDatasheet.Contains(app.ListDatasheetImportData[i].DatasheetName))
                            tempListDataSheet.Add(app.ListDatasheetImportData[i]);
                    
                    var tempColHeaders = app.DictColHeaders.Where(temp => arrSelDatasheet.Contains(temp.Key)).Select(s => s.Value);
                    arrColumnHeader = tempColHeaders.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);

                    foreach (string tempDataSheet in arrSelDatasheet)
                        app.DictColHeaders.Remove(tempDataSheet);

                    app.DictColHeaders.Add(strCSVSavePath, arrColumnHeader);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                File.WriteAllText(strCSVSavePath, sb.AppendLine(string.Join(",", arrColumnHeader)).ToString());
                for (int count = 0; count < tempListDataSheet.Count; count++)
                    File.AppendAllLines(strCSVSavePath, tempListDataSheet[count].ListImportData.Select(temp => string.Join(",", temp)));

                return strCSVSavePath;
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong..!Merge Failed.", "Error!!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ManageAndMergeDatasheet.xaml.cs::MergeCSVDatasheets: " + ex.Message, 0);
            }

            return string.Empty;
        }

        private string MergeXMLDatasheets(string[] arrSelDatasheet)
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Merge Datasheet\Merge XML";
                string xmlSavePath = System.IO.Path.Combine(strDataPath, "XML_" + DateTime.Now.ToString("ddMMyyyy_HHmmss"));
                if (!System.IO.Directory.Exists(xmlSavePath))
                    System.IO.Directory.CreateDirectory(xmlSavePath);

                string[] arrMergeDasheet;
                if (arrSelDatasheet == null)
                    arrMergeDasheet = app.settings.ImportFilePath;
                else
                    arrMergeDasheet = app.settings.ImportFilePath.Where(item => arrSelDatasheet.Contains(item)).ToArray();

                for (int index = 0; index < arrMergeDasheet.Length; index++)
                {
                    string[] aarXMLFiles = Directory.GetFiles(arrMergeDasheet[index], "*.xml", SearchOption.TopDirectoryOnly);
                    foreach (string sourcepath in aarXMLFiles)
                    {
                        string destPath = xmlSavePath + "\\" + Delimon.Win32.IO.Path.GetFileName(sourcepath);
                        Delimon.Win32.IO.File.Copy(sourcepath, destPath, true);
                    }
                }
                return xmlSavePath;
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong..!Merge Failed.", "Error!!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ManageAndMergeDatasheet.xaml.cs::MergeCSVDatasheets: " + ex.Message, 0);
            }

            return string.Empty;
        }
    }
}
