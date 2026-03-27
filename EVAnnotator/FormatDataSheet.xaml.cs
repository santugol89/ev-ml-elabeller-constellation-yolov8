using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for FormatDataSheet.xaml
    /// </summary>
    public partial class FormatDataSheet : Window, INotifyPropertyChanged
    {
        MainWindow app;
        String fileType;
        bool bIsFormated;
        int nExportRow, nTotal;
        List<ClassFormat> listClass;
        List<string> listLines;
        BackgroundWorker BGWorkerFormat;
        List<ClassFormat> ListClassFormat = new List<ClassFormat>();
        List<LineFormat> ListLineFormat = new List<LineFormat>();
        Dictionary<string, string> dictImageLineList = new Dictionary<string, string>();
        string[] DatasheetLoaded = null;
        bool bIsSaveData = false;
        public event PropertyChangedEventHandler PropertyChanged;
        string[] arrSelDatasheet;
        bool bIsDefaultMode = true;
        bool bIsLS3Project = false;

        public FormatDataSheet(MainWindow app, String FileType)
        {
            InitializeComponent();
            this.fileType = FileType;
            this.app = app;
            SwitchMode.IsChecked = false;
            DataContext = this;
            InitializeControls();
            this.SizeChanged += FormatDataSheet_SizeChanged;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void FormatDataSheet_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NotifyPropertyChanged("SetFilterClassHieght");
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            app.OnWorkerMethodComplete("complete");
        }

        private void InitializeControls()
        {
            if (fileType == "CSV")
            {
                lblHeading.Content = "Format CSV DataSheet";
                chkDropDateStamp.Visibility = Visibility.Visible;
                chkDropCorrectionCol.Visibility = Visibility.Visible;
                chkDropDLine.Visibility = app.settings.ClassType != EnumClassType.Segregation? Visibility.Visible : Visibility.Collapsed;
            }
            else if (fileType == "JSON")
            {
                lblHeading.Content = "Format JSON DataSheet";
                chkDropDateStamp.Visibility = Visibility.Collapsed;
                chkDropCorrectionCol.Visibility = Visibility.Collapsed;
                chkDropDLine.Visibility = Visibility.Collapsed;
            }
            //radDefault.IsChecked = true;
            gpClass.Visibility = Visibility.Collapsed;
            gpFormat.Visibility = Visibility.Collapsed;
            gpLine.Visibility = Visibility.Collapsed;
            gpLinePath.Visibility = Visibility.Collapsed;
            chkSelAllLine.IsChecked = true;
            ListClassFormat = app.GetClassFormatList();
            ListClassView.ItemsSource = ListClassFormat;
            InitializeLineCheckBox();
            chkSelAllClass.IsChecked = ListClassFormat.Exists(item => item.IsClassEnable == false) ? false : true;
            btnExportMany.Visibility = Visibility.Collapsed;  //app.settings.ImportFilePath.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
            bIsLS3Project = false; /*app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV") ? true : false;*/
            txtImagePath.Text = app.settings.CSVExportPath;
            SetListBoxProperty();
        }

        private void InitializeLineCheckBox()
        {
            for (int i = 0; i < app.settings.LineList.Length; i++){
                ListLineFormat.Add(new LineFormat{
                    LineName = app.settings.LineList[i].ToString(),
                    IsLineSelected = true
                });
            }
            ListLineView.ItemsSource = ListLineFormat;
        }

        private void SetListBoxProperty(bool isEnable = false)
        {
            var s = new Style(typeof(Telerik.Windows.Controls.RadListBoxItem));
            var disableSetter = new Setter { Property = IsEnabledProperty, Value = isEnable };
            s.Setters.Add(disableSetter);
            listLoadedFile.ItemContainerStyle = s;
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            //MessageBoxResult result = System.Windows.MessageBox.Show("Save Formatting?", "Save Formatting", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //if (result == MessageBoxResult.No){
            //    this.Close();
            //    return;
            //}

            //bIsSaveData = true;
            //btnExportDatasheet_Click(btnExportSingle, null);
            this.Close();
        }

        List<string> fileList = new List<string>();
        public List<string> LoadedFileList
        {
            get
            {
                return app.settings.ImportFilePath.ToList();
            }            
        }

        public string ContentFieldText
        {
            get
            {
                if (bIsLS3Project)
                    return "Field Count";
                else
                    return "Both Field";
            }
        }
        
        public Visibility VisibilityField
        {
            get
            {
                if (!bIsLS3Project)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public string ColumnWidthField
        {
            get
            {
                if (!bIsLS3Project)
                    return "auto";
                else
                    return "0.15*";
            }            
        }

        public double SetMaxColumnHieght
        {
            get
            {
                return SystemParameters.PrimaryScreenHeight - 100;
            }
        }

        public double SetFilterClassHieght
        {
            get
            {
                return SystemParameters.PrimaryScreenHeight - gpDataList.ActualHeight - SwitchMode.ActualHeight - gpFormat.ActualHeight - gpLine.ActualHeight - gpLinePath.ActualHeight - spBottom.ActualHeight -170;
            }
        }

        private void btnExportDatasheet_Click(object sender, RoutedEventArgs e)
        {            
            listClass = ListClassFormat.Where(item => item.IsClassEnable == true).ToList();
            listLines = ListLineFormat.Where(item => item.IsLineSelected == true).Select(temp => temp.LineName).ToList();
            SetExportCountForClass();

            bool bIsClassFormat = false;
            //To check Class filter need or not
            if (listClass.Count == ListClassFormat.Count && !listClass.Exists(item => item.Option != EnumImageType.Default) && !listClass.Any(s => s.ClassCount != s.ModifiedCount))
                bIsClassFormat = true;

            if (bIsDefaultMode || (!chkDropRows.IsChecked.Value && !chkDropCorrectionCol.IsChecked.Value && !chkDropDateStamp.IsChecked.Value
                && !chkDropDLine.IsChecked.Value && bIsClassFormat && listLines.Count == app.settings.LineList.Length)) {
                MessageBoxResult result = MessageBox.Show("Do you wish to Save Datasheet without Formatting?\nIf No click on Custom selection mode and select Format Options.", "Export", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.No)
                    return;
            }
            if (!bIsDefaultMode && !chkDropDLine.IsChecked.Value && txtImagePath.Text.Trim() == ""){
                MessageBox.Show("Please Select Image folder to fetch and add the line name", "Select", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
                dictImageLineList = GetImageLineList();

            if (!app.CheckFileAccessToFormat())
                return;

            string strDataPath = app.settings.StatsFilePath + @"\Temp Files";
            string[] tempFiles = null;
            if (System.IO.Directory.Exists(strDataPath))
                tempFiles = Directory.GetFiles(strDataPath);

            if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                DatasheetLoaded = tempFiles != null && tempFiles.Length > 0? tempFiles : app.settings.ImportFilePath;
            else if(arrSelDatasheet.Length > 0)
            {
                if (tempFiles != null && tempFiles.Length > 0)
                    DatasheetLoaded = tempFiles.Where(temp => arrSelDatasheet.Select(s => Path.GetFileName(s)).Contains(Path.GetFileName(temp))).ToArray();
                else
                    DatasheetLoaded = arrSelDatasheet;
            }

            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            if ((sender as Button).Name == "btnExportSingle")
                BGWorkerFormat.DoWork += bgwDowork_FormatSingleDatasheet;
            else
                BGWorkerFormat.DoWork += bgwDowork_FormatMultiDatasheet;

            BGWorkerFormat.ProgressChanged += app.bgwProgressChange_Load;
            BGWorkerFormat.RunWorkerAsync();
            if(bIsSaveData)
                app.OnWorkerMethodStart_LoadFile(this, "Saving Formatting..");
            else
                app.OnWorkerMethodStart_LoadFile(this);
        }

        private void btnExportClasswise_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Please confirm to continue export classwise csv datasheets?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
                return;

            listClass = ListClassFormat.Where(item => item.IsClassEnable == true).ToList();
            listLines = ListLineFormat.Where(item => item.IsLineSelected == true).Select(temp => temp.LineName).ToList();
            SetExportCountForClass();

            string strDataPath = app.settings.StatsFilePath + @"\Temp Files";
            string[] tempFiles = null;
            if (System.IO.Directory.Exists(strDataPath))
                tempFiles = Directory.GetFiles(strDataPath);

            if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                DatasheetLoaded = tempFiles != null && tempFiles.Length > 0 ? tempFiles : app.settings.ImportFilePath;
            else if (arrSelDatasheet.Length > 0)
            {
                if (tempFiles != null && tempFiles.Length > 0)
                    DatasheetLoaded = tempFiles.Where(temp => arrSelDatasheet.Select(s => Path.GetFileName(s)).Contains(Path.GetFileName(temp))).ToArray();
                else
                    DatasheetLoaded = arrSelDatasheet;
            }

            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            BGWorkerFormat.DoWork += bgwDowork_ExportClasswiseFiles;
            BGWorkerFormat.ProgressChanged += app.bgwProgressChange_Load;
            BGWorkerFormat.RunWorkerAsync();
            app.OnWorkerMethodStart_LoadFile(this);
        }

        /// <summary>
        /// Function to Show the Export stat message after completing export
        /// </summary>
        public void ShowMessageBoxFormat(int nDatasheetCount, string message = "")
        {
            MessageBox.Show("Total " + fileType + " DataSheet : " + nDatasheetCount + "\nTotal Records found : "
                + nTotal + "\nNo. of Dropped Rows : " + (nTotal - nExportRow)
                + "\nNo. of Records Exported : " + nExportRow, "File Export", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            Utilities.LogMessage(nExportRow + " no. of records exported after formating.", 0);
        }

        private void bgwDowork_FormatSingleDatasheet(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadFormat = null;
                if (fileType == "CSV")
                    threadFormat = new Thread(ExportFormattedCSVToSingleFile);

                else if (fileType == "JSON")
                    threadFormat = new Thread(ExportFormattedJSONToSingleFile);

                if (threadFormat != null)
                {
                    threadFormat.IsBackground = true;
                    threadFormat.Start();
                }
            }
        }

        private void bgwDowork_FormatMultiDatasheet(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadFormat = null;
                if (fileType == "CSV")
                    threadFormat = new Thread(ExportFormattedCSVToMultiple);

                else if (fileType == "JSON")
                    threadFormat = new Thread(ExportFormattedJSONToMultiple);

                if(threadFormat != null)
                {
                    threadFormat.IsBackground = true;
                    threadFormat.Start();
                }                    
            }            
        }

        private void bgwDowork_ExportClasswiseFiles(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadFormat = null;
                if (fileType == "CSV")
                    threadFormat = new Thread(ExportFormattedCSVAsClasswise);

                else if (fileType == "JSON")
                    threadFormat = new Thread(ExportFormattedJSONAsClasswise);

                if (threadFormat != null)
                {
                    threadFormat.IsBackground = true;
                    threadFormat.Start();
                }
            }
        }

        /// <summary>
        /// Function to format, merge and export multiple CSV files if any to single file
        /// </summary>
        private void ExportFormattedCSVAsClasswise()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted CSV\classwise_" + DateTime.Now.ToString("ddMMyyyy_HHmmss");
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                nExportRow = 0;
                nTotal = 0;
                StringBuilder sb = new StringBuilder();
                bool bIsCustom = false, bIsDropDate = false, bIsDropCorrectionCol = false, bIsDropLine = false;
                int listFileSelIndex = -1;
                string listFileSelItem = "";
                this.Dispatcher.Invoke(() => {
                    //bIsCustom = radCustom.IsChecked.Value;
                    bIsDropDate = chkDropDateStamp.IsChecked.Value;
                    bIsDropCorrectionCol = chkDropCorrectionCol.IsChecked.Value;
                    bIsDropLine = chkDropDLine.IsChecked.Value;
                    listFileSelIndex = listLoadedFile.SelectedIndex;
                    listFileSelItem = listLoadedFile.SelectedIndex > -1 ? listLoadedFile.SelectedItem.ToString() : "";
                });

                List<ImportDatasheetData> tempListDataSheet = new List<ImportDatasheetData>();
                
                foreach (ImportDatasheetData temp in app.ListDatasheetImportData)
                {
                    List<string[]> tempArray = new List<string[]>(temp.ListImportData);
                    if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                    else if (arrSelDatasheet.Length == 1 && arrSelDatasheet.Contains(temp.DatasheetName))
                    {
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                        break;
                    }
                    else if (arrSelDatasheet.Length > 1 && arrSelDatasheet.Contains(temp.DatasheetName))
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                }

                for (int count = 0; count < tempListDataSheet.Count; count++)
                {
                    string strDatasheetName = tempListDataSheet[count].DatasheetName;
                    nTotal += tempListDataSheet[count].ListImportData.Count;
                    if (!bIsDefaultMode)
                    {
                        DropCustomSelectionCSVRows(tempListDataSheet[count].ListImportData, strDatasheetName, app.DictColHeaders[strDatasheetName]);
                        DropCorrectionOrInsertLineNameCol(tempListDataSheet[count].ListImportData, app.DictColHeaders[strDatasheetName]);
                    }
                    nExportRow += tempListDataSheet[count].ListImportData.Count;

                    //if (bIsDropCorrectionCol)
                    //    app.DictColHeaders[strDatasheetName] = app.DictColHeaders[strDatasheetName].Where(s => !s.Contains("to_be_corrected")).ToArray();

                    //if (bIsDropLine)
                    //    app.DictColHeaders[strDatasheetName] = app.DictColHeaders[strDatasheetName].Where(s => !s.Contains("line")).ToArray();
                }
               
                string[] arrColumnHeader = app.settings.ClassType != EnumClassType.Segregation ? new string[] { "filename", "region_count", "region_shape_attributes", "region_attributes", "to_be_corrected", "line_name" } :
                                                                                new string[] { "filename", "region_count", "segregated_class", "to_be_corrected" };

                if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                    arrColumnHeader = app.DictColHeaders.Values.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);
                else
                {
                    var tempColHeaders = app.DictColHeaders.Where(temp => arrSelDatasheet.Contains(temp.Key)).Select(s => s.Value);
                    arrColumnHeader = tempColHeaders.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);
                }

                sb.Clear();
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                if (!bIsDropDate)
                    sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));

                if (app.settings.ClassType != EnumClassType.Segregation && !Array.Exists(arrColumnHeader, d => d.Contains("line")) && !bIsDropLine)
                {
                    Array.Resize(ref arrColumnHeader, arrColumnHeader.Length + 1);
                    arrColumnHeader[arrColumnHeader.Length - 1] = "line_name";
                }
                if (bIsDropCorrectionCol)
                    arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("to_be_corrected")).ToArray();

                if (bIsDropLine)
                    arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("line")).ToArray();

                string strHeadingLines = sb.AppendLine(string.Join(",", arrColumnHeader)).ToString();
                List<string[]> listAllImportData = new List<string[]>();
                for (int count = 0; count < tempListDataSheet.Count; count++)
                    listAllImportData.AddRange(tempListDataSheet[count].ListImportData);

                foreach(ClassFormat CurclassFormat in listClass)
                {
                    string strClass = CurclassFormat.Alias;
                    string strCSVSavePath = System.IO.Path.Combine(strDataPath, CurclassFormat.ClassName + ".csv");
                    File.WriteAllText(strCSVSavePath, strHeadingLines);

                    StringBuilder sbContent = new StringBuilder();
                    for (int cnt = 0; cnt < listAllImportData.Count; cnt++)
                    {
                        string[] lineSplit = listAllImportData[cnt].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                        bool bValidLine = app.IsValidCSVLine(lineSplit);

                        //Drops only unlabelled rows
                        if (!bValidLine)
                        {
                            listAllImportData.RemoveAt(cnt);
                            cnt--;
                            continue;
                        }

                        string ClassName = app.settings.ClassType != EnumClassType.Segregation ? Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper() :
                                                                        Regex.Match(lineSplit[2], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();

                        if (strClass.ToUpper() == ClassName)
                        {
                            sbContent.AppendLine(string.Join(",", listAllImportData[cnt]));                            
                            listAllImportData.RemoveAt(cnt);
                            cnt--;
                        }
                    }
                    File.AppendAllText(strCSVSavePath, sbContent.ToString());
                }

                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Classwise CSV files has been exported successfully \ninto output folder.", "File Export", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Classwise CSV files has been exported.", 0);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedCSVToSingleFile: " + ex.Message, 0);
            }
        }

        private void ExportFormattedJSONAsClasswise()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted JSON\classwise_" + DateTime.Now.ToString("ddMMyyyy_HHmmss");

                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                bIsFormated = false;
                nExportRow = 0;
                nTotal = 0;
                
                bool bIsCustom = false, bIsDropRows = false;
                this.Dispatcher.Invoke(() =>
                {
                    bIsDropRows = chkDropRows.IsChecked.Value;
                });

                List<object> listAllFileObject = new List<object>();
                for (int index = 0; index < DatasheetLoaded.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(DatasheetLoaded[index])))
                    {
                        string jsonLines = reader.ReadToEnd();

                        JObject jsonObj = JsonConvert.DeserializeObject(jsonLines) as JObject;
                        List<object> listObject = (jsonObj as IEnumerable<object>).ToList();
                        if (listObject.Count <= 0)
                            continue;

                        var objectType = (listObject[0] as JProperty).Value.GetType();
                        if (objectType.Name == "JValue")
                        {
                            listObject.RemoveAt(0);
                        }
                        else if (objectType.Name == "JObject")
                        {
                            if ((listObject[0] as JProperty).Path == "info")
                            {
                                app.OnWorkerMethodComplete("complete");
                                MessageBox.Show("COCO Compatible JSON cannot be formatted! \nPlease select proper JSON for formatting.", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        nTotal += listObject.Count;

                        if (!bIsDefaultMode)
                            listObject = DropUnlabelledJSONRows(listObject);
                        else
                            listObject = (jsonObj as IEnumerable<object>).ToList();

                        nExportRow += listObject.Count;

                        int count = 0;
                        bIsFormated = true;
                        listObject = listObject.Where(obj => ((obj as JProperty).Children().Values().ToList().Count > 1 ?
                                        (obj as JProperty).Children().Values().ToList()[1] as JProperty : null).Value.Count() > 0).ToList();

                        listAllFileObject.AddRange(listObject);                        
                    }                    
                }

                foreach(ClassFormat curClass in listClass)
                {
                    string strClass = curClass.Alias;
                    string strJSONSavePath = System.IO.Path.Combine(strDataPath, curClass.ClassName + ".json");

                    StringBuilder sbContent = new StringBuilder();
                    sbContent.AppendLine("{");
                    sbContent.AppendLine("\"project\" : \"" + strProjectname + "\",");
                    List<ImagePropertyJSON> listTempItems = new List<ImagePropertyJSON>();
                    for (int line = 0; line < listAllFileObject.Count; line++)
                    {
                        JToken tempJToken = listAllFileObject[line] as JToken;
                        ImagePropertyJSON curImageProperty = JsonConvert.DeserializeObject<ImagePropertyJSON>(tempJToken.First().ToString());

                        for (int i = 0; i < curImageProperty.regions.Count; i++)
                        {                            
                            string strAlias = curImageProperty.regions[i].region_attributes.class_name.Split('(', ')').Length > 1 ? curImageProperty.regions[i].region_attributes.class_name.Split('(', ')')[1].ToUpper() :
                                                curImageProperty.regions[i].region_attributes.class_name.Split('(', ')')[0].ToUpper();
                            if (curClass.Alias.ToUpper() != strAlias)
                            {
                                curImageProperty.regions.RemoveAt(i);
                                i--;
                            }                            
                        }

                        if(curImageProperty.regions.Count > 0)
                            listTempItems.Add(curImageProperty);
                    }

                    for(int j = 0; j < listTempItems.Count; j++)
                    {
                        string output = JsonConvert.SerializeObject(listTempItems[j].filename, Newtonsoft.Json.Formatting.Indented);
                        sbContent.AppendLine(output + ":");
                        string strJsonObject = JsonConvert.SerializeObject(listTempItems[j], Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings()
                        {
                            ContractResolver = new IgnoreEmptyEnumerableResolver(),
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        });

                        sbContent.Append(strJsonObject);
                        if(j < listTempItems.Count - 1)
                            sbContent.AppendLine(",");
                    }

                    sbContent.AppendLine("}");

                    if(listTempItems.Count > 0)
                        File.WriteAllText(strJSONSavePath, sbContent.ToString());
                }

                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Classwise JSON files has been exported successfully \ninto output folder.", "File Export", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                if (!bIsSaveData)
                    MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error,
                                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                else
                    MessageBox.Show("Something went wrong..!\nCould not Save Changes.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error,
                                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedJSONToSingleFile: " + ex.Message, 0);
            }
        }

        /// <summary>
        /// Function to format, merge and export multiple CSV files if any to single file
        /// </summary>
        private void ExportFormattedCSVToSingleFile()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted CSV";
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");
                bool bIsSave = false;
                if (!bIsSaveData)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                        saveFileDialog.InitialDirectory = strDataPath;
                        saveFileDialog.Filter = "csv file|*.csv";
                        saveFileDialog.FileName = "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv";

                        System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            strCSVSavePath = saveFileDialog.FileName;
                            bIsSave = true;
                        }
                    });

                    if (!bIsSave)
                    {
                        app.OnWorkerMethodComplete("complete");
                        return;
                    }
                }                

                nExportRow = 0;
                nTotal = 0;
                StringBuilder sb = new StringBuilder();
                bool bIsCustom = false, bIsDropDate = false, bIsDropCorrectionCol = false, bIsDropLine = false;
                int listFileSelIndex = -1;
                string listFileSelItem = "";
                this.Dispatcher.Invoke(() => {
                    //bIsCustom = radCustom.IsChecked.Value;
                    bIsDropDate = chkDropDateStamp.IsChecked.Value;
                    bIsDropCorrectionCol = chkDropCorrectionCol.IsChecked.Value;
                    bIsDropLine = chkDropDLine.IsChecked.Value;
                    listFileSelIndex = listLoadedFile.SelectedIndex;
                    listFileSelItem = listLoadedFile.SelectedIndex > -1? listLoadedFile.SelectedItem.ToString() : "";
                });

                List<ImportDatasheetData> tempListDataSheet = new List<ImportDatasheetData>();
                if(bIsSaveData)
                    tempListDataSheet = arrSelDatasheet == null || arrSelDatasheet.Length == 0? app.ListDatasheetImportData : 
                                        app.ListDatasheetImportData.Where(temp => arrSelDatasheet.Contains(temp.DatasheetName)).ToList();
                else
                {
                    foreach (ImportDatasheetData temp in app.ListDatasheetImportData){
                        List<string[]> tempArray = new List<string[]>(temp.ListImportData);
                        if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                            tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                        else if (arrSelDatasheet.Length == 1 && arrSelDatasheet.Contains(temp.DatasheetName)){
                            tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                            break;
                        }
                        else if (arrSelDatasheet.Length > 1 && arrSelDatasheet.Contains(temp.DatasheetName))
                            tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                    }
                }

                for (int count = 0; count < tempListDataSheet.Count; count++)
                {
                    string strDatasheetName = tempListDataSheet[count].DatasheetName;
                    nTotal += tempListDataSheet[count].ListImportData.Count;
                    if (!bIsDefaultMode)
                    {
                        DropCustomSelectionCSVRows(tempListDataSheet[count].ListImportData, strDatasheetName, app.DictColHeaders[strDatasheetName]);
                        DropCorrectionOrInsertLineNameCol(tempListDataSheet[count].ListImportData, app.DictColHeaders[strDatasheetName]);
                    }
                    nExportRow += tempListDataSheet[count].ListImportData.Count;

                    //if (bIsDropCorrectionCol)
                    //    app.DictColHeaders[strDatasheetName] = app.DictColHeaders[strDatasheetName].Where(s => !s.Contains("to_be_corrected")).ToArray();

                    //if (bIsDropLine)
                    //    app.DictColHeaders[strDatasheetName] = app.DictColHeaders[strDatasheetName].Where(s => !s.Contains("line")).ToArray();
                }

                if (!bIsSaveData)
                {
                    string[] arrColumnHeader = app.settings.ClassType != EnumClassType.Segregation? new string[] { "filename", "region_count", "region_shape_attributes", "region_attributes", "to_be_corrected", "line_name" } : 
                                                                                                    new string[] { "filename", "region_count", "segregated_class", "to_be_corrected" };
                    if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                        arrColumnHeader = app.DictColHeaders.Values.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);
                    else{
                        var tempColHeaders = app.DictColHeaders.Where(temp => arrSelDatasheet.Contains(temp.Key)).Select(s => s.Value);
                        arrColumnHeader = tempColHeaders.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);
                    }

                    sb.Clear();
                    string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                    if (!bIsDropDate)
                        sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));

                    if (app.settings.ClassType != EnumClassType.Segregation && !Array.Exists(arrColumnHeader, d => d.Contains("line")) && !bIsDropLine)
                    {
                        Array.Resize(ref arrColumnHeader, arrColumnHeader.Length + 1);
                        arrColumnHeader[arrColumnHeader.Length - 1] = "line_name";
                    }
                    if (bIsDropCorrectionCol)
                        arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("to_be_corrected")).ToArray();

                    if (bIsDropLine)
                        arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("line")).ToArray();

                    File.WriteAllText(strCSVSavePath, sb.AppendLine(string.Join(",", arrColumnHeader)).ToString());
                    for (int count = 0; count < tempListDataSheet.Count; count++)
                        File.AppendAllLines(strCSVSavePath, tempListDataSheet[count].ListImportData.Select(temp => string.Join(",", temp)));
                }
                    
                app.OnWorkerMethodComplete("complete");
                if(!bIsSaveData)
                    ShowMessageBoxFormat(tempListDataSheet.Count);
                else{
                    app.bIsFormatFile = true;
                    //app.UpdateAugmentationClassList();
                    this.Dispatcher.Invoke(() => this.Close());
                }
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, 
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedCSVToSingleFile: " + ex.Message, 0);
            }
        }

        /// <summary>
        /// Function to format, merge and export multiple JSON files if any to single file
        /// </summary>
        private void ExportFormattedJSONToSingleFile()
        {
            try
            {
                string strDataPath = "";
                if (bIsSaveData){
                    strDataPath = app.settings.StatsFilePath + @"\Temp Files\Format File";
                    if (System.IO.Directory.Exists(strDataPath))
                        System.IO.Directory.Delete(strDataPath, true);
                }
                else
                    strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted JSON";

                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);
                string strJSONSavePath = System.IO.Path.Combine(strDataPath, "json_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".json");

                if (!bIsSaveData)
                {
                    bool bIsSave = false;
                    this.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                        saveFileDialog.InitialDirectory = strDataPath;
                        saveFileDialog.Filter = "json file|*.json";
                        saveFileDialog.FileName = "json_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".json";

                        System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            strJSONSavePath = saveFileDialog.FileName;
                            bIsSave = true;
                        }
                    });

                    if (!bIsSave)
                    {
                        app.OnWorkerMethodComplete("complete");
                        return;
                    }
                }                

                bIsFormated = false;
                nExportRow = 0;
                nTotal = 0;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("\"project\" : \"" + strProjectname + "\",");
                bool bIsCustom = false, bIsDropRows = false;
                this.Dispatcher.Invoke(() =>
                {
                    //bIsCustom = radCustom.IsChecked.Value;
                    bIsDropRows = chkDropRows.IsChecked.Value;
                });
 
                for (int index = 0; index < DatasheetLoaded.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(DatasheetLoaded[index])))
                    {
                        string jsonLines = reader.ReadToEnd();

                        JObject jsonObj = JsonConvert.DeserializeObject(jsonLines) as JObject;
                        //List<object> listObject = (jsonObj.Children().Values() as IEnumerable<object>).ToList();
                        List<object> listObject = (jsonObj as IEnumerable<object>).ToList();
                        if (listObject.Count <= 0)
                            continue;

                        var objectType = (listObject[0] as JProperty).Value.GetType();
                        if (objectType.Name == "JValue")
                        {
                            listObject.RemoveAt(0);
                        }
                        else if (objectType.Name == "JObject")
                        {
                            if ((listObject[0] as JProperty).Path == "info")
                            {
                                app.OnWorkerMethodComplete("complete");
                                MessageBox.Show("COCO Compatible JSON cannot be formatted! \nPlease select proper JSON for formatting.", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        nTotal += listObject.Count;

                        //List<object> listObject = new List<object>();
                        if (!bIsDefaultMode)
                            listObject = DropUnlabelledJSONRows(listObject);
                        else
                            listObject = (jsonObj as IEnumerable<object>).ToList();

                        nExportRow += listObject.Count;

                        int count = 0;
                        bIsFormated = true;
                        while (count < listObject.Count)
                        {
                            string json = JsonConvert.SerializeObject(listObject[count], Formatting.Indented);
                            sb.Append(json);
                            count++;
                            if (count < listObject.Count)
                                sb.Append(",");
                        }
                    }
                    if (index < app.settings.ImportFilePath.Length - 1)
                        sb.Append(",");
                }
                sb.Append("}");
                if (bIsFormated)
                    File.WriteAllText(strJSONSavePath, sb.ToString());

                app.OnWorkerMethodComplete("complete");
                if (!bIsSaveData)
                    ShowMessageBoxFormat(DatasheetLoaded.Length);
                else
                    Dispatcher.Invoke(() => this.Close());
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, 
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                if (!bIsSaveData)
                    MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, 
                                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                else
                    MessageBox.Show("Something went wrong..!\nCould not Save Changes.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error,
                                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedJSONToSingleFile: " + ex.Message, 0);
            }
        }

        /// <summary>
        /// Function to format and export multiple CSV files to seperate file into output folder
        /// </summary>
        private void ExportFormattedCSVToMultiple()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted CSV";
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                StringBuilder sb = new StringBuilder();
                nExportRow = 0;
                nTotal = 0;
                bool bIsCustom = false, bIsDropDate = false, bIsDropCorrectionCol = false, bIsDropLine = false;

                this.Dispatcher.Invoke(() =>
                {
                    //bIsCustom = radCustom.IsChecked.Value;
                    bIsDropDate = chkDropDateStamp.IsChecked.Value;
                    bIsDropCorrectionCol = chkDropCorrectionCol.IsChecked.Value;
                    bIsDropLine = chkDropDLine.IsChecked.Value;
                });
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                List<ImportDatasheetData> tempListDataSheet = new List<ImportDatasheetData>();
                foreach (ImportDatasheetData temp in app.ListDatasheetImportData){
                    List<string[]> tempArray = new List<string[]>(temp.ListImportData);
                    if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                    else if (arrSelDatasheet.Length == 1 && arrSelDatasheet.Contains(temp.DatasheetName)){
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                        break;
                    }
                    else if (arrSelDatasheet.Length > 1 && arrSelDatasheet.Contains(temp.DatasheetName))
                        tempListDataSheet.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                }

                for (int count = 0; count < tempListDataSheet.Count; count++){
                    nTotal += tempListDataSheet[count].ListImportData.Count;
                    if (!bIsDefaultMode)
                    {
                        DropCustomSelectionCSVRows(tempListDataSheet[count].ListImportData, tempListDataSheet[count].DatasheetName, app.DictColHeaders[tempListDataSheet[count].DatasheetName]);
                        DropCorrectionOrInsertLineNameCol(tempListDataSheet[count].ListImportData, app.DictColHeaders[tempListDataSheet[count].DatasheetName]);
                    }
                    nExportRow += tempListDataSheet[count].ListImportData.Count;
                }


                for (int count = 0; count < tempListDataSheet.Count; count++)
                {
                    sb.Clear();
                    if (!bIsDropDate)
                        sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));

                    string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + (count + 1).ToString("(0)") + ".csv");
                    string[] arrColumnHeader = app.DictColHeaders[tempListDataSheet[count].DatasheetName];
                    if (!Array.Exists(arrColumnHeader, d => d.Contains("line")) && !bIsDropLine)
                    {
                        Array.Resize(ref arrColumnHeader, arrColumnHeader.Length + 1);
                        arrColumnHeader[arrColumnHeader.Length - 1] = "line_name";
                    }

                    if (bIsDropCorrectionCol)
                        arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("to_be_corrected")).ToArray();

                    if (bIsDropLine)
                        arrColumnHeader = arrColumnHeader.Where(s => !s.Contains("line")).ToArray();

                    File.WriteAllText(strCSVSavePath, sb.AppendLine(string.Join(",", arrColumnHeader)).ToString());
                    File.AppendAllLines(strCSVSavePath, tempListDataSheet[count].ListImportData.Select(temp => string.Join(",", temp)));
                }

                app.OnWorkerMethodComplete("complete");
                ShowMessageBoxFormat(tempListDataSheet.Count);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedCSVToMultiple: " + ex.Message, 0);
            }
        }

        /// <summary>
        /// Function to format and export multiple JSON files to seperate file into output folder
        /// </summary>
        private void ExportFormattedJSONToMultiple()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Formatted Data\Formatted JSON";
                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                bIsFormated = false;
                nExportRow = 0;
                nTotal = 0;
                StringBuilder sb = new StringBuilder();
                bool bIsCustom = false, bIsDropRows = false;
                this.Dispatcher.Invoke(() =>
                {
                    //bIsCustom = radCustom.IsChecked.Value;
                    bIsDropRows = chkDropRows.IsChecked.Value;
                });

                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                for (int index = 0; index < DatasheetLoaded.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(DatasheetLoaded[index])))
                    {
                        string jsonLines = reader.ReadToEnd();

                        JObject jsonObj = JsonConvert.DeserializeObject(jsonLines) as JObject;
                        //List<object> listObject = (jsonObj.Children().Values() as IEnumerable<object>).ToList();
                        List<object> listObject = (jsonObj as IEnumerable<object>).ToList();
                        if (listObject.Count <= 0)
                            continue;

                        var objectType = (listObject[0] as JProperty).Value.GetType();
                        if (objectType.Name == "JValue")
                        {
                            listObject.RemoveAt(0);
                        }
                        else if (objectType.Name == "JObject")
                        {
                            if ((listObject[0] as JObject).Path == "info")
                            {
                                app.OnWorkerMethodComplete("complete");
                                MessageBox.Show("COCO Compatible JSON cannot be formatted! Please select proper JSON for formatting.", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        nTotal += listObject.Count;
                        
                        //List<object> listObject = new List<object>();
                        if (!bIsDefaultMode)
                            listObject = DropUnlabelledJSONRows(listObject);
                        else
                            listObject = (jsonObj as IEnumerable<object>).ToList();

                        nExportRow += listObject.Count;

                        int count = 0;
                        bIsFormated = true;

                        sb.Clear();
                        sb.AppendLine("{");
                        sb.AppendLine("\"project\" : \"" + strProjectname + "\",");
                        while (count < listObject.Count)
                        {
                            string json = JsonConvert.SerializeObject(listObject[count], Formatting.Indented);
                            sb.Append(json);
                            count++;
                            if (count < listObject.Count)
                                sb.Append(",");
                        }
                        sb.Append("}");
                        string strJSONSavePath = System.IO.Path.Combine(strDataPath, "json_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + (index + 1).ToString("(0)") + ".json");
                        File.WriteAllText(strJSONSavePath, sb.ToString());
                    }
                }
                app.OnWorkerMethodComplete("complete");
                ShowMessageBoxFormat(DatasheetLoaded.Length);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, 
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, 
                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ExportFormattedJSONToMultiple: " + ex.Message, 0);
            }
        }

        //// <summary>
        /// Function to format in custom mode where filtering class with count of single phase, phase contrast and both images as well as line names
        /// It also removes unlabelled rows of csv datasheet if drop unlabelled rows checkbox is selected
        /// </summary>     
        private void DropCustomSelectionCSVRows(List<string[]> listCSVLines, string DatasheetName, string[] arrColumnHeader)
        {
            bool bIsDropRows = false;
            bool bIsClassFormat = false;
            this.Dispatcher.Invoke(() =>
            {
                bIsDropRows = chkDropRows.IsChecked.Value;
            });

            //To check Class filter need or not
            if (listClass.Count == ListClassFormat.Count && !listClass.Exists(item => item.Option != EnumImageType.Default) && !listClass.Any(s => s.ClassCount != s.ModifiedCount))
                bIsClassFormat = true;

            if (!bIsDropRows && bIsClassFormat && listLines.Count == app.settings.LineList.Length)
                return;

            for (int lines = 0; lines < listCSVLines.Count; lines++)
            {
                string[] lineSplit = listCSVLines[lines].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                bool bValidLine = app.IsValidCSVLine(lineSplit);

                //Drops only unlabelled rows
                if ((!bValidLine && bIsDropRows) || app.IsColHeaderLine(listCSVLines[lines])){
                    listCSVLines.RemoveAt(lines);
                    lines--;
                    continue;
                }

                //Drops Selected Class or Line rows 
                if ((bIsClassFormat && listLines.Count == app.settings.LineList.Length) || !bValidLine)
                    continue;

                //string ClassID = Regex.Match(lineSplit[3], @"\b[:]\s*[0-9]+").ToString().Replace(":", "").Trim();
                string ClassName = app.settings.ClassType != EnumClassType.Segregation? Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper() :
                                                                                        Regex.Match(lineSplit[2], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                string lineName = string.Empty;
                if (Array.Exists(arrColumnHeader, d => d.Contains("line")))
                {
                    if (arrColumnHeader.Contains("to_be_corrected"))
                        lineName = lineSplit.Length > 5 ? lineSplit[5].Replace(" ","").ToUpper() : "";
                    else
                        lineName = lineSplit.Length > 4 ? lineSplit[4].Replace(" ", "").ToUpper() : "";
                }

                string strOption = "";
                ClassFormat curClassFormat = listClass.FirstOrDefault(item => item.Alias.ToUpper() == ClassName);
                if (curClassFormat != null)
                {
                    strOption = curClassFormat.Option == EnumImageType.PhaseContrast ? app.settings.PhaseContrast : curClassFormat.Option == EnumImageType.SingleSpot ? app.settings.SinglePhase : "both";

                    if(strOption == "both" || lineSplit[0].Contains(strOption))
                        curClassFormat.ExportCount--;
                }

                if (!bIsClassFormat && listLines.Count < app.settings.LineList.Length)
                {
                    if (curClassFormat == null || (lineName != "" && !listLines.ConvertAll(d => d.Replace(" ", "")).Contains(lineName)))
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                    else if(strOption != "both" && !lineSplit[0].Contains(strOption))
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                    else if (curClassFormat.ExportCount < 0)
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                }
                else if (!bIsClassFormat && listLines.Count == app.settings.LineList.Length)
                {
                    if (curClassFormat == null)
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                    else if(strOption != "both" && !lineSplit[0].Contains(strOption))
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                    else if(curClassFormat.ExportCount < 0)
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                }
                else if (bIsClassFormat && listLines.Count < app.settings.LineList.Length)
                {
                    if (lineName != "" && !listLines.ConvertAll(d => d.Replace(" ", "")).Contains(lineName))
                    {
                        listCSVLines.RemoveAt(lines);
                        lines--;
                        UpdateImageListBox(lineSplit, ClassName, DatasheetName);
                    }
                }
            }
        }

        private void UpdateImageListBox(string[] lineSplit, string strAlias, string DatasheetName)
        {
            if (!bIsSaveData)   
                return;

            ImageListBox curImageBox = app.ProcessedImageBox.FirstOrDefault(item => item.ImageBoxName == lineSplit[0].Trim());
            if (curImageBox == null)
                return;

            var tempImageClass = curImageBox.ListImageClass.FirstOrDefault(item => Regex.Replace(item.ShapeCoordinates, @"[""{} ]", "") == Regex.Replace(lineSplit[2], @"[""{} ]", "") && item.ClassAlias.ToUpper() == strAlias);
            Dispatcher.Invoke(() =>
            {
                if (tempImageClass != null)
                    curImageBox.ListImageClass.Remove(tempImageClass);
            });
            
            char strImageType = curImageBox.ImageBoxName.Contains(app.settings.SinglePhase) ? 'S' : curImageBox.ImageBoxName.Contains(app.settings.PhaseContrast) ? 'P' : ' ';
            foreach (ClassFolderStat temp in app.ListClassFolderStat)
            {
                if(temp.ImportDatasheetName == DatasheetName && temp.ClassAliasName.ToUpper() == strAlias)
                {
                    temp.ClassCount--;
                    if (strImageType == 'S')
                        temp.SingleSpotCount--;
                    else if (strImageType == 'P')
                        temp.PhaseContrastCount--;
                }
            }
        }

        //// <summary>
        /// Function to format in custom mode where dropping correction column if drop correction check box selected
        /// Function also insert line name column w.r.t the image path contains any line folder name matching with image name in csv file
        /// </summary>  
        private void DropCorrectionOrInsertLineNameCol(List<string[]> listCSVLines, string[] arrColumnHeader)
        {
            bool bIsDropCorrectionCol = false, bIsDropLine = false;
            string strImagePath = "";
            this.Dispatcher.Invoke(() =>
            {
                bIsDropCorrectionCol = chkDropCorrectionCol.IsChecked.Value;
                bIsDropLine = chkDropDLine.IsChecked.Value;
                strImagePath = txtImagePath.Text.Trim();
            });

            if (!bIsDropCorrectionCol && !bIsDropLine && strImagePath == "")
                return;

            int corrIndex = arrColumnHeader.ToList().IndexOf("to_be_corrected");
            int lineIndex = corrIndex != -1 && bIsDropCorrectionCol && Array.Exists(arrColumnHeader, d => d.Contains("line")) ?
                            Array.IndexOf(arrColumnHeader, arrColumnHeader.Where(s => s.Contains("line")).FirstOrDefault()) - 1 :
                            Array.IndexOf(arrColumnHeader, arrColumnHeader.Where(s => s.Contains("line")).FirstOrDefault());

            //string[] arrLoadedFiles = !bIsDropLine ? app.GetAllFilesFromDirectory(strImagePath).ToArray() : null;

            for (int lines = 0; lines < listCSVLines.Count; lines++)
            {
                string[] lineSplit = listCSVLines[lines];
                
                if (lineSplit.Length > 4 && corrIndex != -1 && !(lineSplit[4].ToLower().Contains("no") || lineSplit[4].ToLower().Contains("yes")))
                    lineSplit[4] = "";

                if (lineSplit.Length != arrColumnHeader.Length)
                    Array.Resize(ref lineSplit, arrColumnHeader.Length);
                if (arrColumnHeader.Length < 6 && lineSplit.Length > 4)
                    Array.Resize(ref lineSplit, arrColumnHeader.Length);

                //string strLineName = !bIsDropLine && arrLoadedFiles != null ? app.GetLineNameFromSelectedImagePath(lineSplit[0], arrLoadedFiles) : "";
                string strLineName = !bIsDropLine && dictImageLineList.Count > 0? (dictImageLineList.ToList().Exists(item => item.Key == lineSplit[0].Trim())? 
                                    dictImageLineList.Where(item => item.Key == lineSplit[0].Trim()).FirstOrDefault().Value : "") : "";
                List<string> temp = lineSplit.ToList();

                if (bIsDropCorrectionCol && corrIndex != -1)
                    temp.RemoveAt(corrIndex);

                if (lineIndex != -1)
                    temp.RemoveAt(lineIndex);

                //Add or Drop Line Nme
                if (app.settings.ClassType != EnumClassType.Segregation && !bIsDropLine)
                    temp.Add(strLineName);

                listCSVLines[lines] = temp.ToArray();
            }
        }

        /// <summary>
        /// Function to format in custom mode where filtering class with count of single phase, phase contrast and both images
        /// It also removes unlabelled rows of json datasheet if drop unlabelled rows checkbox is selected
        /// </summary>    
        private List<object> DropUnlabelledJSONRows(List<object> listObject)
        {
           // List<object> listObject = (listJsonObjects as IEnumerable<object>).ToList();
            bool bIsDropRows = false;
            bool bIsClassFormat = false;
            this.Dispatcher.Invoke(() =>
            {
                bIsDropRows = chkDropRows.IsChecked.Value;
            });
            //try
            //{
            if (listClass.Count == ListClassFormat.Count && !listClass.Exists(item => item.Option != EnumImageType.Default))
                bIsClassFormat = true;

            if (bIsClassFormat && !bIsDropRows)
                return listObject;

            if (bIsDropRows)
            {
                listObject = listObject.Where(obj => ((obj as JProperty).Children().Values().ToList().Count > 1 ?
                    (obj as JProperty).Children().Values().ToList()[1] as JProperty : null).Value.Count() > 0).ToList();

                //listObject = (listJsonObjects.Children() as IEnumerable<object>).ToList()
                //    .Where(obj => ((obj as JProperty).Children().Values().ToList().Count > 1 ?
                //    (obj as JProperty).Children().Values().ToList()[1] as JProperty : null).Value.Count() > 0).ToList();
            }

            if (!bIsClassFormat)
            {
                List<object> listDroppedObj = new List<object>();
                for (int i = 0; i < listObject.Count; i++)
                {
                    JToken tempJToken = listObject[i] as JToken;
                    if ((tempJToken.Children().Values().ToList()[1] as JProperty).Value.Count() == 0 && !bIsDropRows)
                    {
                        listDroppedObj.Add(tempJToken);
                        continue;
                    }

                    string ImageName = (((JProperty)tempJToken.Children().Values().ToList()[0]).Value as JToken).ToString();
                    var listProperty = (tempJToken.Children().Values().ToArray()[1] as JProperty).Children().Values().ToArray();
                    int j = 0;
                    while (j < listProperty.Count())
                    {
                        object[] arrObj = ((JToken)listProperty[j]).Children().Values().ToArray();
                        var jsonClassAttribute = JsonConvert.DeserializeObject<JsonClassAttributes>(arrObj[1].ToString().Replace("\n", "").Replace("\r", ""));

                        string strClassName = jsonClassAttribute.ClassName.Split('(', ')').Length > 1 ? jsonClassAttribute.ClassName.Split('(', ')')[1].ToUpper() : 
                                                jsonClassAttribute.ClassName.Split('(', ')')[0].ToUpper();
                        string strOption = "";
                        ClassFormat curClassFormat = listClass.FirstOrDefault(item => item.Alias.ToUpper() == strClassName);
                        if (curClassFormat != null)
                        {
                            strOption = curClassFormat.Option == EnumImageType.PhaseContrast ? app.settings.PhaseContrast : curClassFormat.Option == EnumImageType.SingleSpot ? app.settings.SinglePhase : "both";
                            if (strOption == "both" || ImageName.Contains(strOption))
                                curClassFormat.ExportCount--;
                        }

                        if (curClassFormat == null || (strOption != "both" && !ImageName.Contains(strOption)))
                            (tempJToken.Children().Values().ToArray()[1] as JProperty).Children().Values().Where(item => item == (JToken)listProperty[j]).ToList().ForEach(a => a.Remove());
                        else if(curClassFormat.ExportCount < 0)
                            (tempJToken.Children().Values().ToArray()[1] as JProperty).Children().Values().Where(item => item == (JToken)listProperty[j]).ToList().ForEach(a => a.Remove());

                        j++;
                    }

                    if ((tempJToken.Children().Values().ToList()[1] as JProperty).Value.Count() > 0)
                        listDroppedObj.Add(tempJToken);
                }
                listObject = listDroppedObj;
            }
            return listObject;
            //}

            //catch(Exception ex)
            //{
            //    return listObject;
            //}
        }

        //private void radSelection_Click(object sender, RoutedEventArgs e)
        //{
        //    if (radDefault.IsChecked.Value)
        //    {
        //        gpClass.Visibility = Visibility.Collapsed;
        //        gpFormat.Visibility = Visibility.Collapsed;
        //        gpLine.Visibility = Visibility.Collapsed;
        //        gpLinePath.Visibility = Visibility.Collapsed;
        //        chkDropRows.IsChecked = false;
        //        chkDropCorrectionCol.IsChecked = false;
        //        chkDropDateStamp.IsChecked = false;
        //        SetListBoxProperty();
        //        listLoadedFile.SelectedIndex = -1;
        //        foreach (LineFormat curLine in ListLineFormat)
        //            curLine.IsLineSelected = true;
        //    }

        //    else
        //    {
        //        gpClass.Visibility = Visibility.Visible;
        //        gpFormat.Visibility = Visibility.Visible;
        //        gpLine.Visibility = fileType == "JSON" || ListLineFormat.Count == 0? Visibility.Collapsed : Visibility.Visible;
        //        gpLinePath.Visibility = fileType == "JSON" || ListLineFormat.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        //        SetListBoxProperty(true);
        //    }
        //}

        private void btnBrowseImagePath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = app.settings.LoadImagePath;

            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                txtImagePath.Text = folderDialog.SelectedPath;
        }

        private void chkDropLine_Click(object sender, RoutedEventArgs e)
        {
            if (ListLineFormat.Count == 0)
                return;

            if(chkDropDLine.IsChecked.Value){
                gpLine.Visibility = Visibility.Collapsed;
                gpLinePath.Visibility = Visibility.Collapsed;
            }                
            else{
                gpLine.Visibility = Visibility.Visible;
                gpLinePath.Visibility = Visibility.Visible;
            }
        }

        private void radOption_Click(object sender, RoutedEventArgs e)
        {
            string strOption = (sender as RadioButton).Tag.ToString();
            ClassFormat curClassFormat = (sender as RadioButton).DataContext as ClassFormat;
            curClassFormat.Option = strOption == "1" ? EnumImageType.Default : strOption == "2" ? EnumImageType.SingleSpot : EnumImageType.PhaseContrast;
        }

        private void SetExportCountForClass()
        {
            foreach(ClassFormat curClass in ListClassFormat){
                curClass.ExportCount = curClass.Option == EnumImageType.SingleSpot ? curClass.ExportSinglespot :
                                        curClass.Option == EnumImageType.PhaseContrast ? curClass.ExportPhaseContrast : curClass.ModifiedCount;
            }
        }

        /// <summary>
        /// Function to Get loaded images and their respective line name if present in path into dictionary
        /// </summary>  
        private Dictionary<string, string> GetImageLineList()
        {
            Dictionary<string, string> tempList = new Dictionary<string, string>();
            string[] arrLoadedFiles = app.GetAllFilesFromDirectory(txtImagePath.Text.Trim()).ToArray();
            arrLoadedFiles = arrLoadedFiles.DistinctBy(item => System.IO.Path.GetFileName(item)).ToArray();
            foreach (string ImageName in arrLoadedFiles)
            {
                string strLineName = ImageName.Split('\\').Where(item => app.settings.LineList.Contains(item.ToUpper()) ||
                                app.settings.LineList.Select(lines => lines.Replace(" ", "")).Contains(item.ToUpper())).FirstOrDefault();

                if (strLineName != null)
                    strLineName = strLineName.Replace(" ", "").ToUpper();
                else
                    strLineName = "";

                tempList.Add(System.IO.Path.GetFileName(ImageName), strLineName);
            }

            return tempList;
        }

        private void chkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).Name == "chkSelAllClass")
            {
                foreach (ClassFormat curClass in ListClassFormat)
                    curClass.IsClassEnable = (sender as CheckBox).IsChecked.Value ? true : false;
            }
            else
            {
                foreach (LineFormat curLine in ListLineFormat)
                    curLine.IsLineSelected = (sender as CheckBox).IsChecked.Value ? true : false;
            }
        }

        private void txtCount_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if ((sender as TextBox).Text == "")
                    (sender as TextBox).Text = "0";
            }
            ClassFormat curClass = (sender as TextBox).DataContext as ClassFormat;
            curClass.ModifiedCount = (sender as TextBox).Text != "" ? Convert.ToInt32((sender as TextBox).Text) : 0;
        }

        private void listLoadedFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (app.settings.ImportFilePath == null || app.settings.ImportFilePath.Length == 1)
                return;

            arrSelDatasheet = listLoadedFile.SelectedItems.Cast<string>().ToArray();
            ListClassFormat = app.GetClassFormatList(arrSelDatasheet);
            ListClassView.ItemsSource = ListClassFormat;
            btnExportMany.Visibility = arrSelDatasheet == null || arrSelDatasheet.Length < 2? Visibility.Collapsed: Visibility.Visible;
        }

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void SwitchMode_Click(object sender, MouseButtonEventArgs e)
        {
            bIsDefaultMode = !bIsDefaultMode;
            SwitchMode.Content = bIsDefaultMode ? "Default" :"Custom";
            Utilities.LogMessage(SwitchMode.IsChecked.Value.ToString());
            if (bIsDefaultMode)
            {
                SwitchMode.IsChecked = true;
                gpClass.Visibility = Visibility.Collapsed;
                gpFormat.Visibility = Visibility.Collapsed;
                gpLine.Visibility = Visibility.Collapsed;
                gpLinePath.Visibility = Visibility.Collapsed;
                chkDropRows.IsChecked = false;
                chkDropCorrectionCol.IsChecked = false;
                chkDropDateStamp.IsChecked = false;
                SetListBoxProperty();
                listLoadedFile.SelectedIndex = -1;
                foreach (LineFormat curLine in ListLineFormat)
                    curLine.IsLineSelected = true;

                btnExportClasswise.Visibility = Visibility.Collapsed;
            }

            else
            {
                SwitchMode.IsChecked = false;
                gpClass.Visibility = Visibility.Visible;
                gpFormat.Visibility = Visibility.Visible;
                gpLine.Visibility = fileType == "JSON" || ListLineFormat.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                gpLinePath.Visibility = fileType == "JSON" || ListLineFormat.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                SetListBoxProperty(true);
                btnExportClasswise.Visibility = Visibility.Visible;
            }
            Utilities.LogMessage(SwitchMode.IsChecked.Value.ToString());
        }

        private void txtCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if ((sender as TextBox).Text == "0" || (sender as TextBox).SelectedText == (sender as TextBox).Text)
                (sender as TextBox).Text = "";

            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
            ClassFormat curClass = (sender as TextBox).DataContext as ClassFormat;
            if (Convert.ToInt32((sender as TextBox).Text + e.Text) > curClass.ClassCount)
                e.Handled = true;

        }
    }

    public class ClassFormat : INotifyPropertyChanged
    {
        public string ClassName { get; set; }

        public string Alias { get; set; }

        public int ClassCount { get; set; }

        public int SingleSpotCount { get; set; }

        public int PhaseContrastCount { get; set; }

        public int ExportCount { get; set; }

        private EnumImageType _option = EnumImageType.Default;
        public EnumImageType Option
        {
            get
            {
                return _option;
            }
            set
            {
                _option = value;
                NotifyPropertyChanged("ExportSinglespot");
                NotifyPropertyChanged("ExportPhaseContrast");
            }
        }

        private int _exportSinglespot = 0;
        public int ExportSinglespot
        {
            get
            {
                if(Option == EnumImageType.SingleSpot)
                {
                    if (ModifiedCount > SingleSpotCount)
                        return SingleSpotCount;
                    else
                        return ModifiedCount;
                }
                else
                    return SingleSpotCount;
            }
            set
            {
                _exportSinglespot = value;
                NotifyPropertyChanged("ExportSinglespot");
            }
        }

        private int _exportPhaseContrast = 0;
        public int ExportPhaseContrast
        {
            get
            {
                if (Option == EnumImageType.PhaseContrast)
                {
                    if (ModifiedCount > PhaseContrastCount)
                        return PhaseContrastCount;
                    else
                        return ModifiedCount;
                }
                else
                    return PhaseContrastCount;
            }
            set
            {
                _exportPhaseContrast = value;
                NotifyPropertyChanged("ExportPhaseContrast");
            }
        }

        private int _modifiedCount = 0;
        public int ModifiedCount
        {
            get
            {
                return _modifiedCount;
            }
            set
            {
                _modifiedCount = value;
                NotifyPropertyChanged("ModifiedCount");
                NotifyPropertyChanged("ExportSinglespot");
                NotifyPropertyChanged("ExportPhaseContrast");
            }
        }

        public Visibility _visibilityExportField = Visibility.Visible;
        public Visibility VisibilityExportField
        {
            get
            {
                return _visibilityExportField;
            }
            set
            {
                _visibilityExportField = value;
                NotifyPropertyChanged("VisibilityExportField");
            }
        }

        public string _columnWidthExportField = "0.15*";
        public string ColumnWidthExportField
        {
            get
            {
                return _columnWidthExportField;
            }
            set
            {
                _columnWidthExportField = value;
                NotifyPropertyChanged("ColumnWidthExportField");
            }
        }

        private bool _isClassEnable = true;
        public bool IsClassEnable
        {
            get
            {
                return _isClassEnable;
            }
            set
            {
                _isClassEnable = value;
                NotifyPropertyChanged("IsClassEnable");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    class LineFormat : INotifyPropertyChanged
    {
        public string LineName { get; set; }

        private bool _isLineSelected = true;
        public bool IsLineSelected
        {
            get
            {
                return _isLineSelected;
            }
            set
            {
                _isLineSelected = value;
                NotifyPropertyChanged("IsLineSelected");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum EnumImageType
    {
        Default, SingleSpot, PhaseContrast
    }

    public class ClassAttributeJSON
    {
        public string project { get; set; }
        public ImagePropertyJSON ImageNames { get; set; }

        public ClassAttributeJSON()
        {
            ImageNames = new ImagePropertyJSON();
        }
    }

    public class ImagePropertyJSON
    {
        public string filename { get; set; }
        public List<ClassRegions> regions { get; set; }

        public ImagePropertyJSON()
        {
            regions = new List<ClassRegions>();
        }
    }

    public class ClassRegions
    {
        public ClassShape shape_attributes { get; set; }
        public ClassRegionAttributes region_attributes { get; set; }

        public ClassRegions()
        {
            shape_attributes = new ClassShape();
            region_attributes = new ClassRegionAttributes();
        }
    }

    public class ClassShape
    {
        public string name { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public List<double> all_points_x { get; set; }
        public List<double> all_points_y { get; set; }

        public ClassShape()
        {
            all_points_x = new List<double>();
            all_points_y = new List<double>();
        }
    }

    public class ClassRegionAttributes
    {
        [JsonProperty("class id")]
        public string class_id { get; set; }

        [JsonProperty("class name")]
        public string class_name { get; set; }
        public string review { get; set; }

    }
}
