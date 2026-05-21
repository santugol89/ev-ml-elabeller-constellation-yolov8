using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using MoreLinq;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public List<ImportDatasheetData> ListDatasheetImportData = new List<ImportDatasheetData>();
        public List<DataViolation> ListDataViolation = new List<DataViolation>();
        public List<ClassFolderStat> ListClassFolderStat = new List<ClassFolderStat>();
        public ManualResetEvent labelEvent = new ManualResetEvent(false);
        Thread threadCheckLabelling;
        public bool bThreadKilled;
        public BackgroundWorker bgWorker;
        public int MinuteSavingWorkStats = 5;
        public int MinuteUpdateLabellingStats = 1;
        //public DateTime timeCheckSavingWorkStats = DateTime.Now;
        public bool isSaved = false;
        public ManualResetEvent SaveEvent = new ManualResetEvent(false);
        public Dictionary<string, string[]> DictColHeaders = new Dictionary<string, string[]>();
        public Dictionary<string, string> DictModifiedClass = new Dictionary<string, string>();
        public List<ModifiedClass> ListModifiedClass = new List<ModifiedClass>();

        /// <summary>
        /// Function to update Total Image present stat in left side window  
        /// </summary>
        private int totImagesPresent = 0;
        public int TotalImagesPresent
        {
            get
            {
                return totImagesPresent;
            }
            set
            {
                totImagesPresent = value;
                NotifyPropertyChanged("TotalImagesPresent");
            }
        }

        /// <summary>
        /// Function to update Total Image loaded stat in left side window  
        /// </summary>
        private int totImagesLoaded = 0;
        public int TotalImagesLoaded
        {
            get
            {
                return totImagesLoaded;
            }
            set
            {
                totImagesLoaded = value;
                NotifyPropertyChanged("TotalImagesLoaded");
            }
        }

        /// <summary>
        /// Function to update Total Duplicate image stats in left side window 
        /// </summary>
        private int totDuplicateImages = 0;
        public int TotalDuplicateImages
        {
            get
            {
                return totDuplicateImages;
            }
            set
            {
                totDuplicateImages = value;
                NotifyPropertyChanged("TotalDuplicateImages");
            }
        }

        /// <summary>
        /// Function to update Total Datasheet stats in left side window 
        /// </summary>
        private int totDataSheet = 0;
        public int TotalDataSheet
        {
            get
            {
                return totDataSheet;
            }
            set
            {
                totDataSheet = value;
                NotifyPropertyChanged("TotalDataSheet");
            }
        }

        /// <summary>
        /// Function to update Total records stats in left side window 
        /// </summary>
        private int totRecordsFound = 0;
        public int TotalRecordFound
        {
            get
            {
                return totRecordsFound;
            }
            set
            {
                totRecordsFound = value;
                NotifyPropertyChanged("TotalRecordFound");
            }
        }

        /// <summary>
        /// Function to update Total violated records stat in left side window 
        /// </summary>
        private int totViolationFound = 0;
        public int TotalViolationFound
        {
            get
            {
                return totViolationFound;
            }
            set
            {
                totViolationFound = value;
                NotifyPropertyChanged("TotalViolationFound");
            }
        }

        /// <summary>
        /// Function to enable or disable visibility of Validation stat in left side window by changing settings in Setting window
        /// </summary>
        private Visibility _validationStatVisibility = Visibility.Collapsed;
        public Visibility ValidationStatVisibility
        {
            get
            {
                return _validationStatVisibility;
            }
            set
            {
                _validationStatVisibility = value;
                NotifyPropertyChanged("ValidationStatVisibility");
            }
        }

        private Visibility _colorROINoteVisibility = Visibility.Collapsed;
        public Visibility ColorROIInfoVisibility
        {
            get
            {
                return _colorROINoteVisibility;
            }
            set
            {
                _colorROINoteVisibility = value;
                NotifyPropertyChanged("ColorROIInfoVisibility");
            }
        }

        /// <summary>
        /// Function to update Total unlabelled Images stat in left side window 
        /// </summary>
        private int unlabelledImages = 0;
        public int TotalUnlabelledImages
        {
            get
            {
                return unlabelledImages;
            }
            set
            {
                unlabelledImages = value;
                NotifyPropertyChanged("TotalUnlabelledImages");
            }
        }

        /// <summary>
        /// Function to update Total labelled images stat in left side window 
        /// </summary>
        private int labelledImages = 0;
        public int TotalLabelledImages
        {
            get
            {
                return labelledImages;
            }
            set
            {
                labelledImages = value;
                NotifyPropertyChanged("TotalLabelledImages");
            }
        }

        /// <summary>
        /// Function to update Total correction images stat in left side window 
        /// </summary>
        private int correctionImages = 0;
        public int TotalCorrectionImages
        {
            get
            {
                return correctionImages;
            }
            set
            {
                correctionImages = value;
                NotifyPropertyChanged("TotalCorrectionImages");
            }
        }

        /// <summary>
        /// Function to update Total labelled regions stat in left side window 
        /// </summary>
        private int multiClassLabelled = 0;
        public int TotalMultiClassLabelled
        {
            get
            {
                return multiClassLabelled;
            }
            set
            {
                multiClassLabelled = value;
                NotifyPropertyChanged("TotalMultiClassLabelled");
            }
        }

        private double multiClassRowHeight = 30;
        public double MultiClassRowHeight
        {
            get
            {
                if (settings.ClassType == EnumClassType.Rectangle || settings.ClassType == EnumClassType.Polyline)
                    return 30;
                else
                    return 0;
            }
            set
            {
                NotifyPropertyChanged("MultiClassRowHeight");
            }
        }

        private Brush _groundTruthStroke = Brushes.Red;
        public Brush GroundTruthStroke
        {
            get
            {
                return _groundTruthStroke;
            }
        }

        private Brush _groundTruthHighLihtStroke = Brushes.Blue;
        public Brush GroundTruthHighLihtStroke
        {
            get
            {
                return _groundTruthHighLihtStroke;
            }
        }

        private Brush _predictedStroke = Brushes.DarkViolet;
        public Brush PredictedStroke
        {
            get
            {
                return _predictedStroke;
            }
        }

        private Brush _predictHighLightStroke = Brushes.LightYellow;
        public Brush PredictHighLightStroke
        {
            get
            {
                return _predictHighLightStroke;
            }
        }

        /// <summary>
        /// Function to check violated fields by using some rule check in CSV loaded 
        /// </summary>
        public void LoadViolatedDataFromCSV(string[] arrSelDatasheet = null)
        {
            ListDataViolation = arrSelDatasheet == null? new List<DataViolation>() : ListDataViolation.Where(item => !arrSelDatasheet.Contains(item.ImagePathName)).ToList();
            for (int index = 0; index < ListDatasheetImportData.Count; index++)
            {
                if (arrSelDatasheet != null && !arrSelDatasheet.Contains(ListDatasheetImportData[index].DatasheetName))
                    continue;
                Dispatcher.Invoke(() =>
                {
                    progressBar.pbStatus.Maximum = ListDatasheetImportData[index].ListImportData.Count * 2;
                    progressBar.pbStausText.Text = "Validating CSV files..";
                });

                int skipRowCount = File.ReadAllLines(settings.ImportFilePath[index]).ToList().Count - ListDatasheetImportData[index].ListImportData.Count;
                CheckFilenameViolation(ListDatasheetImportData[index].ListImportData, ListDatasheetImportData[index].DatasheetName, skipRowCount);
                CheckRegionViolation(ListDatasheetImportData[index].ListImportData, ListDatasheetImportData[index].DatasheetName, skipRowCount);
            }

            TotalViolationFound = ListDataViolation.Count;
        }

        /// <summary>
        /// Function to Refresh side menu image list while in filtered mode when change to correction/labelled
        /// </summary>
        private void RefreshListBoxImages()
        {
            if (cmbSort.SelectedIndex <= 0)
                return;

            int curIndex = listBoxImages.SelectedIndex;
            if (cmbClassFilter.SelectedIndex == 0)
            {
                listBoxImages.SelectionChanged -= ListBoxImages_SelectionChanged;
                listBoxImages.ItemsSource = cmbSort.SelectedIndex > 0 ? new ObservableCollection<ImageMenu>(ImageMenuList.Where(item => item.MenuItemBrush == ImageMenuBrushes[cmbSort.SelectedIndex - 1])) : ImageMenuList;
                listBoxImages.SelectionChanged += ListBoxImages_SelectionChanged;
            }
            else if (cmbClassFilter.SelectedIndex > 0)
                cmbClassFilter_SelectionChanged(null, null);

            if (listBoxImages.Items.Count > 0) {
                listBoxImages.SelectedIndex = listBoxImages.Items.Count == curIndex ? curIndex - 1 : curIndex;
            }
            else
            {
                listBoxImages.SelectedIndex = -1;
                ResetWindow();
            }
        }

        /// <summary>
        /// Function to check file name and region count field violation by using some rule check in CSV file 
        /// </summary>
        private void CheckFilenameViolation(List<string[]> listCSVLines, string strcsvPath, int skipRowCount)
        {
            bool valid = true;
            for (int lines = 0; lines < listCSVLines.Count; lines++)
            {
                Dispatcher.Invoke(() => progressBar.pbStatus.Value = lines);
                string[] lineSplit = listCSVLines[lines];
                if (IsColHeaderLine(listCSVLines[lines]))
                {
                    skipRowCount++;
                    continue;
                }

                int n;
                //Rule for Filename violation : Check for string & .bmp extension
                //Rule for Filename violation : Check for leading and trailing space present
                if (lineSplit.Length == 0 || int.TryParse(lineSplit[0], out n) || System.IO.Path.GetExtension(lineSplit[0].Trim()) != ".bmp" || Regex.IsMatch(lineSplit[0], "^\\s") || Regex.IsMatch(lineSplit[0], "\\s$"))
                    valid = false;

                if (!valid)
                {
                    InsertListDataViolation(lineSplit[0].Trim(), strcsvPath, (lines + skipRowCount + 1), 1);
                    valid = true;
                }

                //Rule for region count violation : Check for any character & leading and trailing spaces
                if (lineSplit.Length < 2 || !int.TryParse(lineSplit[1], out n) || Regex.IsMatch(lineSplit[1], "^\\s") || Regex.IsMatch(lineSplit[1], "\\s$")) {
                    InsertListDataViolation(lineSplit[0].Trim(), strcsvPath, (lines + skipRowCount + 1), 2);
                }
            }
        }

        /// <summary>
        /// Function to check region attribute field violation by using some rule check in CSV file 
        /// </summary>
        private void CheckRegionViolation(List<string[]> listCSVLines, string strcsvPath, int skipRowCount)
        {
            //Rules for Region shape and class violation : 
            //Check for leading and trailing space present
            //Allow for single space after ','
            //Check for double space present any 
            //Allow for single space inside the polyline points and check for double space in between
            for (int lines = 0; lines < listCSVLines.Count; lines++)
            {
                Dispatcher.Invoke(() => progressBar.pbStatus.Value = listCSVLines.Count + lines);
                string[] lineSplit = listCSVLines[lines];

                if (IsColHeaderLine(listCSVLines[lines])) {
                    skipRowCount++;
                    continue;
                }                

                if (settings.ClassType == EnumClassType.Rectangle || settings.ClassType == EnumClassType.Polyline)
                {
                    if (lineSplit.Length < 3 || !ValidStringCheck(lineSplit[2]))
                        InsertListDataViolation(lineSplit[0].Trim(), strcsvPath, (lines + skipRowCount + 1), 3);
                    if (lineSplit.Length < 4 || !ValidStringCheck(lineSplit[3], ""))
                        InsertListDataViolation(lineSplit[0].Trim(), strcsvPath, (lines + skipRowCount + 1), 4);
                }
                else
                {
                    if (lineSplit.Length < 3 || !ValidStringCheck(lineSplit[2], ""))
                        InsertListDataViolation(lineSplit[0].Trim(), strcsvPath, (lines + skipRowCount + 1), 4);
                }                
            }
        }

        /// <summary>
        /// Function to Insert Data Violation to the list for filename, region count, Shape attributes and class attributes 
        /// </summary>
        private void InsertListDataViolation(string FileName, string CSVPath, int RowNumber, int indexColumn)
        {
            DataViolation curImageViolate = ListDataViolation.Find(item => item.ImageFileName == FileName &&
                                                item.ImagePathName == CSVPath && item.ViolatedRow == RowNumber);
            if (curImageViolate == null)
            {
                curImageViolate = new DataViolation(FileName, CSVPath, RowNumber);
                ListDataViolation.Add(curImageViolate);
            }

            if (indexColumn == 1)
                curImageViolate.FilenameViolated = true;
            else if (indexColumn == 2)
                curImageViolate.RegionCountViolated = true;
            else if (indexColumn == 3)
                curImageViolate.ShapeViolated = true;
            else if (indexColumn == 4)
                curImageViolate.RegionClassViolated = true;
        }

        /// <summary>
        /// Function to check class name attribute field violation by using some rule check in CSV file 
        /// </summary>
        private bool ValidStringCheck(string classAttributes, string strType = "Shape")
        {
            string[] SplitString = Regex.Split(classAttributes, @"(?<!,[^[]+\[[^]]+),");
            Regex reg = new Regex(@"\[(.*?)\]");
            //Rule : Check for leading and trailing space present
            //if (lineSplit[2].Substring(0, 1) == " " || lineSplit[2].Substring(lineSplit[2].Length - 1, 1) == " ")
            if (SplitString.ToList().Exists(item => Regex.IsMatch(item, "\\s{") || Regex.IsMatch(item, "}\\s") || Regex.IsMatch(item, "{\\s") || Regex.IsMatch(item, "\\s\\s}")))
                return false;

            //Rule: check for space present inside double quote
            if (SplitString.ToList().Exists(item => Regex.IsMatch(item.Trim(), "\"\"\\s") || Regex.IsMatch(item.Trim(), "\\s\"\"")))
                return false;

            //Rule: check for double space present in leading & single space present in trailing end
            if (SplitString.ToList().Exists(item => Regex.IsMatch(item, "^\\s\\s") || Regex.IsMatch(item, "\\s$")))
                return false;

            //Rule: Check for double space present after ':' & single space present before ':'
            if (SplitString.ToList().Exists(item => Regex.IsMatch(item, ":\\s\\s") || Regex.IsMatch(item, "\\s:")))
                return false;

            if (strType != "" && SplitString[0].Substring(SplitString[0].LastIndexOf(':') + 1).Replace("\"", "") == "polyline")
            {
                //Rule : Check for leading and trailing space present inside polygon point array
                if (SplitString.ToList().Exists(item => Regex.IsMatch(reg.Match(item).ToString(), "^\\[\\s") || Regex.IsMatch(reg.Match(item).ToString(), "\\s\\]$")))
                    return false;

                //Rule : Check for double space present after ',' & single space present before ',' inside polygon points array
                if (SplitString.ToList().Exists(item => Regex.IsMatch(reg.Match(item).ToString(), ",\\s\\s") || Regex.IsMatch(reg.Match(item).ToString(), "\\s,")))
                    return false;
            }
            else if (strType == "")
            {
                if (SplitString.Length == 1 && Regex.Replace(SplitString[0], @"[^0-9a-zA-Z:.,]", "") != "")
                    return false;
                else if (SplitString.Length > 1 && settings.bIsValidatewithID)
                {
                    //Rule : Check Class ID and Name matches with class file selected
                    string tempID = Regex.Replace(SplitString[1], @"[^0-9a-zA-Z:.,]", "").Split(':').Length > 1 ? Regex.Replace(SplitString[1], @"[^0-9a-zA-Z:.,]", "").Split(':')[1] : "";
                    string tempName = Regex.Replace(SplitString[0], @"[^0-9a-zA-Z:.,]", "").Split(':').Length > 1 ? Regex.Replace(SplitString[0], @"[^0-9a-zA-Z:.,]", "").Split(':')[1] : "";
                    if (string.IsNullOrEmpty(tempID) || string.IsNullOrEmpty(tempName))
                        return false;
                    int n;
                    bool bIsIntCheck = Int32.TryParse(tempID, out n);

                    if (!bIsIntCheck)
                    {
                        string temp;
                        temp = tempID;
                        tempID = tempName;
                        tempName = temp;
                    }
                    int classID;
                    if (!Int32.TryParse(tempID, out classID))
                        return false;

                    if (!settings.dictEVSupervisorClass.Keys.Contains(classID))
                        return false;
                    if ((settings.dictEVSupervisorClass[classID].Split('(', ')').Count() > 1 ? settings.dictEVSupervisorClass[classID].Split('(', ')')[1].ToUpper() : "") != tempName.ToUpper())
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Function to match the datasheet loaded and images loaded and update the labelling stats 
        /// </summary>
        public void ImageClassMatching()
        {
            if (ImageMenuList == null || ImageMenuList.Count == 0 || ProcessedImageBox == null || ProcessedImageBox.Count == 0)
                return;

            int i = 0;
            Dispatcher.Invoke(() => {
                progressBar.pbStatus.Maximum = ProcessedImageBox.Count;
                progressBar.pbStausText.Text = "Matching images...";
            });
            foreach (ImageListBox curImageBox in ProcessedImageBox)
            {
                Dispatcher.Invoke(() => progressBar.pbStatus.Value = i++);
                ImageMenu curImageMenu = ImageMenuList.FirstOrDefault(item => item.ImageName == curImageBox.ImageBoxName);
                
                if (curImageMenu != null) {
                    curImageMenu.ImageBox = curImageBox;                    
                }
            }
            //LoadAllVisualizationGraphs();
        }

        /// <summary>
        /// Function to reset stats count, image loaded and processed image class list 
        /// </summary>
        public void CleanupLoadedData(bool bIsResetAll = false)
        {
            SetFileAttributeNormal();
            if ((ProcessedImageBox.Count > 0 && settings.ApplicationMode == "Normal") || bIsResetAll)
                ProcessedImageBox.Clear();

            ListDatasheetImportData = new List<ImportDatasheetData>();
            ListClassFolderStat = new List<ClassFolderStat>();
            ListDataViolation = new List<DataViolation>();
            ListAugmentTypeClass = new List<AugmentTypeClass>();
            IsVisibleAgmentButton = Visibility.Collapsed;
            IsVisibleMultiCSVExport = Visibility.Collapsed;
            Dispatcher.Invoke(() => {
                ListAugmentationView.ItemsSource = ListAugmentTypeClass;
                txtBatchSize.Text = "";
                tgAH.IsChecked = false;
                tgAV.IsChecked = false;
                tgAN.IsChecked = false;
                tgAR.IsChecked = false;
                tgAT.IsChecked = false;
                tgAB.IsChecked = false;
            });
            bIsFormatFile = false;

            ClearStatTempFiles();
            lock (ImageMenuList) {
                Dispatcher.Invoke(() => {
                    if (ImageMenuList.Count > 0) {
                        foreach (ImageMenu curImageMenu in ImageMenuList)
                            curImageMenu.ImageBox = new ImageListBox(curImageMenu.ImageName);
                    }
                });
            }

            TotalDataSheet = 0;
            TotalRecordFound = 0;
            TotalViolationFound = 0;

            TotalUnlabelledImages = 0;
            TotalLabelledImages = 0;
            TotalCorrectionImages = 0;
            TotalMultiClassLabelled = 0;
            SourceTotalCount = 0;
        }

        /// <summary>
        /// Function to Set controls of application to Normal mode/test mode
        /// </summary>
        public void SetApplicationMenuControls()
        {           
            if (settings.ClassType == EnumClassType.Segregation)
            {
                ImportButton.Visibility = Visibility.Collapsed;
                ImportMultitButton.Visibility = Visibility.Visible;
                borderQuickPallette.Visibility = Visibility.Collapsed;
                rbgFormat.Visibility = Visibility.Visible;
                rbgROI.Visibility = Visibility.Visible;
                SegregateAllButton.Visibility = Visibility.Visible;
            }
            else
            {
                ImportButton.Visibility = Visibility.Collapsed;
                ImportMultitButton.Visibility = Visibility.Visible;
                borderQuickPallette.Visibility = Visibility.Visible;
                rbgFormat.Visibility = Visibility.Visible;
                rbgROI.Visibility = Visibility.Visible;
                SegregateAllButton.Visibility = Visibility.Collapsed;
            }

            bool bIsPatchCore = settings.Architecture.Contains(settings.PatchcoreAlias);
            btnAugmentation.Visibility = bIsPatchCore ? Visibility.Collapsed : Visibility.Visible;
            DataSplit.Text = bIsPatchCore ? "Train/Test Data Splitter" : "Train/Val/Test Data Splitter";
            DataSplit.ToolTip = bIsPatchCore ? "Split Data into train/test" : "Split Data into train/val/test";
        }
        /// <summary>
        /// Function to update Labelling statistics continuously using thread and 
        /// change the color of image name in Side menu list w.r.t labelled, unlabelled and to_be_correction images
        /// </summary>
        DateTime dtAutoSaveTime = DateTime.Now;
        DateTime dtAutoLabelStats = DateTime.Now;
        public bool bIsLoadLabellingGraph = true;
        private void CheckLabellingThread()
        {
            try
            {
                while (true)
                {
                    bool triggered = labelEvent.WaitOne(2000);
                    if (triggered && ImageMenuList != null && ImageMenuList.Count > 0)
                    {
                        lock (ImageMenuList)
                        {
                            ImageMenu[] listTemp = ImageMenuList.ToArray();
                            int tempTotUnlabelled = 0;
                            int tempTotLabelled = 0;
                            int tempTotCorrection = 0;
                            int tempTotMultiClass = 0;

                            foreach (ImageMenu curImageMenu in listTemp.AsParallel())
                            {
                                if (curImageMenu.ImageBox.ListImageClass.Count == 0)
                                {
                                    curImageMenu.MenuItemBrush = ImageMenuBrushes[0];
                                    tempTotUnlabelled++;
                                    continue;
                                }
                                else if (curImageMenu.ImageBox.ListImageClass.Select(temp1 => temp1.Reviewed).Contains(true))
                                {
                                    curImageMenu.MenuItemBrush = ImageMenuBrushes[2];
                                    tempTotCorrection++;
                                }
                                else
                                {
                                    curImageMenu.MenuItemBrush = ImageMenuBrushes[1];
                                    tempTotLabelled++;
                                }
                                tempTotMultiClass += curImageMenu.ImageBox.ListImageClass.Count;
                            }

                            TotalUnlabelledImages = tempTotUnlabelled;
                            TotalLabelledImages = tempTotLabelled;
                            TotalCorrectionImages = tempTotCorrection;
                            TotalMultiClassLabelled = tempTotMultiClass;
                        }

                        if(bIsLoadLabellingGraph || (DateTime.Now - dtAutoLabelStats).TotalMinutes >= MinuteUpdateLabellingStats)
                        {
                            dtAutoLabelStats = DateTime.Now;
                            LoadLabellingStatisticsData();
                            bIsLoadLabellingGraph = false;
                        }
                        if ((DateTime.Now - dtAutoSaveTime).Minutes >= MinuteSavingWorkStats)
                            AutoSaveWorkEvent(null, null);
                    }
                }
            }

            catch (Exception ex)
            {
                SaveEvent.Set();
                Utilities.LogMessage("CheckLabellingThread: " + ex.Message, 9);
            }
        }

        /// <summary>
        /// Function to display and hide status bar label for automatic saved work messages etc
        /// </summary>
        public void ShowStatusBarLabel(string LabelNote, int DisplayTime = 3, bool bIsSaveWork = true)
        {
            this.Dispatcher.Invoke(() =>
            {
                Visibility tempVisiblity = StatusNoteVisiblity;
                string tempLabel = lblStatusNote.Content != null ? lblStatusNote.Content.ToString() : "";
                StatusNoteVisiblity = Visibility.Visible;
                lblStatusNote.Content = LabelNote;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Collapsed;
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(DisplayTime);
                timer.Tick += delegate {
                    StatusNoteVisiblity = tempVisiblity;
                    lblStatusNote.Content = tempLabel;
                    lblAugmentStatus.Visibility = tabSideBar.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
                    lblAutoPilotStatus.Visibility = tabSideBar.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
                    lblImgAnalyzeStatus.Visibility = tabSideBar.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;

                    timer.Stop();
                };
                timer.Start();
                if(bIsSaveWork)
                    LastSavedWorkTime = DateTime.Now.ToShortTimeString();
            });
        }

        private string _lastWorkSavedTime = "";
        public string LastSavedWorkTime
        {
            get
            {
                return _lastWorkSavedTime;
            }

            set
            {
                _lastWorkSavedTime = value;
                NotifyPropertyChanged("LastSavedWorkTime");
            }
        }
        /// <summary>
        /// Function to start thread for Auto save work for every 5 minutes
        /// </summary>
        private void AutoSaveWorkEvent(object sender, EventArgs e)
        {
            dtAutoSaveTime = DateTime.Now;
            bool isSaving = SaveEvent.WaitOne(1000);
            //bool triggered = labelEvent.WaitOne(1000);
            if (isSaving)
            {
                Thread threadSaveStat = new Thread(() => SaveProcessedImageStats("Auto"));
                threadSaveStat.IsBackground = true;
                threadSaveStat.Priority = ThreadPriority.Lowest;
                threadSaveStat.Start();
            }
        }

        /// <summary>
        /// Function to automatically save work for every 5 minutes/ manually by selecting save work
        /// </summary>
        public void SaveProcessedImageStats(string strSaveType = "Manual")
         {
            try
            {
                if (ImageMenuList.Count == 0 || ProcessedImageBox.Count == 0)
                    return;

                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                {
                    return;
                }
                //string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
                string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";

                SaveEvent.Reset();
                if (!Directory.Exists(Workdir))
                    Directory.CreateDirectory(Workdir);

                string[] StatsFile = Directory.GetFiles(Workdir, "*Savedata*.bin");
                string serializationFile = Path.Combine(Workdir, "Savedata_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");

                using (MemoryStream stream = new MemoryStream())
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    string projectKey = settings.CurrentProject;
                    string Architecture = settings.Architecture;
                    bformatter.Serialize(stream, projectKey);
                    bformatter.Serialize(stream, Architecture);
                    bformatter.Serialize(stream, settings.LoadImagePath);
                    bformatter.Serialize(stream, TotalImagesPresent);
                    bformatter.Serialize(stream, TotalImagesLoaded);
                    bformatter.Serialize(stream, TotalDuplicateImages);
                    bformatter.Serialize(stream, ImageMenuList.Count);
                    for (int count = 0; count < ImageMenuList.Count; count++)
                    {
                        ImageMenu curImageMenu = ImageMenuList[count] as ImageMenu;
                        bformatter.Serialize(stream, curImageMenu.ImagePath);
                        bformatter.Serialize(stream, curImageMenu.ImageName);
                        bformatter.Serialize(stream, curImageMenu.ImageSlNo);
                    }

                    bformatter.Serialize(stream, ProcessedImageBox.Count);
                    for (int i = 0; i < ProcessedImageBox.Count; i++)
                    {
                        ImageListBox curImageBox = ProcessedImageBox[i] as ImageListBox;
                        bformatter.Serialize(stream, curImageBox.ImageBoxName);
                        bformatter.Serialize(stream, curImageBox.ImageHeight);
                        bformatter.Serialize(stream, curImageBox.Imagewidth);

                        bformatter.Serialize(stream, curImageBox.ListImageClass.Count);
                        for (int j = 0; j < curImageBox.ListImageClass.Count; j++)
                        {
                            ImageClass curImageClass = curImageBox.ListImageClass[j] as ImageClass;
                            bformatter.Serialize(stream, curImageClass.ClassIndex);
                            bformatter.Serialize(stream, curImageClass.ClassName);
                            bformatter.Serialize(stream, curImageClass.ClassAlias);
                            bformatter.Serialize(stream, curImageClass.XCoordinate);
                            bformatter.Serialize(stream, curImageClass.YCoordinate);
                            bformatter.Serialize(stream, curImageClass.Width);
                            bformatter.Serialize(stream, curImageClass.Height);
                            bformatter.Serialize(stream, curImageClass.ShapeCoordinates);
                            bformatter.Serialize(stream, curImageClass.Shape);
                            bformatter.Serialize(stream, curImageClass.All_Points_X);
                            bformatter.Serialize(stream, curImageClass.All_Points_Y);
                            bformatter.Serialize(stream, curImageClass.Reviewed);


                            bformatter.Serialize(stream, curImageClass.HighLightStroke.ToString());
                            bformatter.Serialize(stream, curImageClass.SelectionStroke.ToString());
                            string str_score = string.IsNullOrEmpty(curImageClass.Score) ? "" : curImageClass.Score;
                            bformatter.Serialize(stream, str_score);
                            bformatter.Serialize(stream, curImageClass.DataTypeMode);
                            bformatter.Serialize(stream, curImageClass.ImportDatasheetName != null? curImageClass.ImportDatasheetName : "");
                        }
                    }

                    //Delete the old file
                    if (StatsFile.Length > 0)
                    {
                        foreach (string file in StatsFile)
                            File.Delete(file);
                    }

                    //Save to new file
                    Stream FileStream = File.Open(serializationFile, FileMode.Create);
                    stream.WriteTo(FileStream);
                    FileStream.Close();
                }

                if (strSaveType == "Manual")
                    OnWorkerMethodComplete("Complete");
                
                ShowStatusBarLabel("Last work has been saved into cache successfully");
                Utilities.LogMessage("Work saved in path " + Workdir);
                SaveEvent.Set();
            }

            catch (Exception ex)
            {
                if (strSaveType == "Manual")
                    OnWorkerMethodComplete("Complete");
                //to find the exception line number
                var lineNumber = 0;
                const string lineSearch = ":line ";
                var index = ex.StackTrace.LastIndexOf(lineSearch);
                if (index != -1)
                {
                    var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                    if (int.TryParse(lineNumberText, out lineNumber))
                    {
                    }
                }
                var r = lineNumber;
                Dispatcher.Invoke(() => Utilities.LogMessage("SaveProcessedImageStats: @ line number: " + lineNumber + " " +ex.Message, 9));
                //Dispatcher.Invoke(() => Utilities.LogMessage("SaveProcessedImageStats: " + ex.Message, 9));
                SaveEvent.Set();
            }
        }

        /// <summary>
        /// Function to load saved work from application stats path
        /// </summary>
        public void LoadProcessedImageStats()
        {
            try
            {
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                
                //string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
                string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";
                string[] StatsFile = Directory.GetFiles(Workdir, "*Savedata*.bin");
                string deSerializFile = System.IO.Path.Combine(Workdir, StatsFile[0]);
                var converter = new System.Windows.Media.BrushConverter();

                using (Stream stream = File.Open(deSerializFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    string projectKey = (string)bformatter.Deserialize(stream);
                    string Architecture = (string)bformatter.Deserialize(stream);

                    if (settings.CurrentProject != projectKey && settings.Architecture != Architecture){
                        OnWorkerMethodComplete("Complete");
                        System.Windows.MessageBox.Show("Mismatch between last saved work and project selected..!\nPlease select proper project from File->Settings.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    settings.LoadImagePath = (string)bformatter.Deserialize(stream);

                    if(!Directory.Exists(settings.LoadImagePath))
                    {
                        OnWorkerMethodComplete("Complete");
                        System.Windows.MessageBox.Show("Loading Last saved work failed. Image Path does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    TotalImagesPresent = (int)bformatter.Deserialize(stream);
                    TotalImagesLoaded = (int)bformatter.Deserialize(stream);
                    TotalDuplicateImages = (int)bformatter.Deserialize(stream);

                    settings.LoadedImageSize = 0;
                    int imageCount = (int)bformatter.Deserialize(stream);
                    Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = imageCount);

                    for (int count = 0; count < imageCount; count++)
                    {
                        Dispatcher.Invoke(() => progressBar.pbStatus.Value = count);
                        lock (ImageMenuList)
                        {
                            string strImagePath = (string)bformatter.Deserialize(stream);
                            string strImageName = (string)bformatter.Deserialize(stream);
                            ImageMenu curImageMenu = new ImageMenu(strImageName);
                            curImageMenu.ImagePath = strImagePath;
                            curImageMenu.ImageName = strImageName;
                            curImageMenu.ImageSlNo = (string)bformatter.Deserialize(stream);

                            if (!File.Exists(strImagePath))
                                continue;

                            this.Dispatcher.Invoke(() =>
                            {
                                ImageMenuList.Add(curImageMenu);
                            });

                            //Calculate the image size for loaded images
                            try
                            {
                                Alphaleonis.Win32.Filesystem.FileInfo file = new Alphaleonis.Win32.Filesystem.FileInfo(strImagePath);
                                settings.LoadedImageSize += Convert.ToUInt64(file.Length);
                            }

                            catch { }
                        }
                    }

                    lock (ImageMenuList)
                    {
                        for (int count = 0; count < ImageMenuList.Count; count++)
                            ImageMenuList[count].ImageSlNo = (count + 1).ToString();
                    }

                    settings.LoadedImagefiles = GetAllFilesFromDirectory(settings.LoadImagePath);
                    List<string> listDistinctImages = settings.LoadedImagefiles.DistinctBy(item => System.IO.Path.GetFileName(item)).ToList();
                    int nDuplicateCount = settings.LoadedImagefiles.Count - listDistinctImages.Count;                   

                    TotalImagesPresent = settings.LoadedImagefiles.Count;
                    TotalImagesLoaded = ImageMenuList.Count;
                    TotalDuplicateImages = nDuplicateCount;

                    int processCount = (int)bformatter.Deserialize(stream);
                    Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = processCount);
                    for (int i = 0; i < processCount; i++)
                    {
                        Dispatcher.Invoke(() => progressBar.pbStatus.Value = i);

                        string strImageName = (string)bformatter.Deserialize(stream);
                        ImageListBox curImageBox = new ImageListBox(strImageName);
                        curImageBox.ImageHeight = (int)bformatter.Deserialize(stream);
                        curImageBox.Imagewidth = (int)bformatter.Deserialize(stream);

                        int classCount = (int)bformatter.Deserialize(stream);
                        for (int j = 0; j < classCount; j++)
                        {
                            string classIndex = (string)bformatter.Deserialize(stream);
                            string className = (string)bformatter.Deserialize(stream);
                            ImageClass curImageClass = new ImageClass(classIndex, className);
                            curImageClass.ClassAlias = (string)bformatter.Deserialize(stream);
                            curImageClass.XCoordinate = (Double)bformatter.Deserialize(stream);
                            curImageClass.YCoordinate = (Double)bformatter.Deserialize(stream);
                            curImageClass.Width = (Double)bformatter.Deserialize(stream);
                            curImageClass.Height = (Double)bformatter.Deserialize(stream);
                            curImageClass.ShapeCoordinates = (string)bformatter.Deserialize(stream);
                            curImageClass.Shape = (EnumSelectedShape)bformatter.Deserialize(stream);
                            curImageClass.All_Points_X = (List<Double>)bformatter.Deserialize(stream);
                            curImageClass.All_Points_Y = (List<Double>)bformatter.Deserialize(stream);
                            curImageClass.Reviewed = (bool)bformatter.Deserialize(stream);

                            var HighlightStroke = (Brush)converter.ConvertFromString((string)bformatter.Deserialize(stream));
                            var SelectionStroke = (Brush)converter.ConvertFromString((string)bformatter.Deserialize(stream));

                            curImageClass.HighLightStroke = HighlightStroke;
                            curImageClass.SelectionStroke = SelectionStroke;
                            curImageClass.Score = (string)bformatter.Deserialize(stream);
                            curImageClass.DataTypeMode = (EnumModeData)bformatter.Deserialize(stream);
                            curImageClass.ImportDatasheetName = (string)bformatter.Deserialize(stream);
                            curImageBox.ListImageClass.Add(curImageClass);
                        }
                        ProcessedImageBox.Add(curImageBox);
                    }
                }

                ImageClassMatching();                   //Matching ImageList and Processed Images
                this.Dispatcher.Invoke(() =>
                {
                    listBoxImages.ItemsSource = ImageMenuList;
                    labelEvent.Set();
                    SaveEvent.Set();
                    bIsLoadLabellingGraph = true;
                    SPSorting.Visibility = Visibility.Visible;
                    SPSearch.Visibility = Visibility.Visible;
                    cmbSort.SelectionChanged -= cmbSort_SelectionChanged;
                    cmbSort.SelectedIndex = 0;
                    cmbSort.SelectionChanged += cmbSort_SelectionChanged;
                    cmbClassFilter.SelectionChanged -= cmbClassFilter_SelectionChanged;
                    cmbClassFilter.SelectedIndex = 0;
                    cmbClassFilter.SelectionChanged += cmbClassFilter_SelectionChanged;
                    txtSearchText.TextChanged -= txtSearchText_TextChanged;
                    txtSearchText.Clear();
                    txtSearchText.TextChanged += txtSearchText_TextChanged;

                    LoadAugmentationStatHistory();
                });

                OnWorkerMethodComplete("Complete");
                ShowStatusBarLabel("Last saved work has been loaded successfully",3,false);
                Utilities.LogMessage("Last saved work has been loaded from path " + Workdir);
            }

            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    listBoxImages.ItemsSource = ImageMenuList;
                    labelEvent.Set();
                    SaveEvent.Set();
                    SPSorting.Visibility = Visibility.Visible;
                    SPSearch.Visibility = Visibility.Visible;
                    cmbSort.SelectionChanged -= cmbSort_SelectionChanged;
                    cmbSort.SelectedIndex = 0;
                    cmbSort.SelectionChanged += cmbSort_SelectionChanged;
                    cmbClassFilter.SelectionChanged -= cmbClassFilter_SelectionChanged;
                    cmbClassFilter.SelectedIndex = 0;
                    cmbClassFilter.SelectionChanged += cmbClassFilter_SelectionChanged;
                    txtSearchText.TextChanged -= txtSearchText_TextChanged;
                    txtSearchText.Clear();
                    txtSearchText.TextChanged += txtSearchText_TextChanged;
                });
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Error while loading last saved work..!", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("LoadSavedWorkHistory " + ex.Message, 9);
            }
        }

        /// <summary>
        /// Function to Set the file attributes from readonly to normal 
        /// </summary>
        public void SetFileAttributeNormal()
        {
            if (settings.ImportFilePath != null)
            {
                for (int i = 0; i < settings.ImportFilePath.Length; i++) {
                    if (File.Exists(settings.ImportFilePath[i]))
                        System.IO.File.SetAttributes(settings.ImportFilePath[i], System.IO.FileAttributes.Normal);
                }                    
            }
        }

        public string GetLineNameFromSelectedImagePath(string strFilename, string[] arrLoadedFiles)
        {
            string strLineName = "";

            string strFetchFile = arrLoadedFiles.FirstOrDefault(s => s.Contains(strFilename));
            if (!string.IsNullOrEmpty(strFetchFile))
            {
                strLineName = strFetchFile.Split('\\').Where(item => settings.LineList.Contains(item.ToUpper()) ||
                                settings.LineList.Select(lines => lines.Replace(" ", "")).Contains(item.ToUpper())).FirstOrDefault();
            }

            if (strLineName != null)
                return strLineName.Replace(" ", "").ToUpper();
            else
                return "";
        }

        /// <summary>
        /// Function to fetch all image files in selected folder and load to application path
        /// </summary>
        public List<string> GetAllFilesFromDirectory(string LoadImagePath)
        {
            List<string> listFiles = new List<string>();
            string[] filesInCurrent = null;
            string[] fileFiltertype = new string[] { ".bmp", ".jpg", ".jpeg", ".png", ".tif" };
            try
            {
                filesInCurrent = Directory.GetFiles(LoadImagePath, "*.*", SearchOption.TopDirectoryOnly).
                                                 Where(i => fileFiltertype.Contains(Path.GetExtension(i))).ToArray();
                listFiles.AddRange(filesInCurrent);

                string[] arrTemp = Directory.GetDirectories(LoadImagePath, "*.*", SearchOption.TopDirectoryOnly).
                              Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.System) && !d.Contains("Output Data")).ToArray();

                int j = 0;
                List<string> listTemp = new List<string>();
                while (j < arrTemp.Length)
                {
                    try
                    {
                        listTemp.Add(arrTemp[j]);
                        listTemp.AddRange(Directory.GetDirectories(arrTemp[j], "*.*", SearchOption.AllDirectories).Where(d => !d.Contains("Output Data")));
                        j++;
                    }

                    catch (Exception ex) {
                        //to find the exception line number
                        var lineNumber = 0;
                        const string lineSearch = ":line ";
                        var index = ex.StackTrace.LastIndexOf(lineSearch);
                        if (index != -1)
                        {
                            var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                            if (int.TryParse(lineNumberText, out lineNumber))
                            {
                            }
                        }
                        var r = lineNumber;
                        Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory:@ line number: " + lineNumber + " " + ex.Message, 9);
                        Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory: " + arrTemp[j] + ":" + ex.Message, 9);
                        j++;
                    }
                }

                for (int i = 0; i < listTemp.Count; i++)
                {
                    try
                    {
                        filesInCurrent = Directory.GetFiles(listTemp[i], "*.*", SearchOption.TopDirectoryOnly).
                                                 Where(f => fileFiltertype.Contains(Path.GetExtension(f))).ToArray();
                        listFiles.AddRange(filesInCurrent);
                    }

                    catch (Exception ex) {
                        //to find the exception line number
                        var lineNumber = 0;
                        const string lineSearch = ":line ";
                        var index = ex.StackTrace.LastIndexOf(lineSearch);
                        if (index != -1)
                        {
                            var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                            if (int.TryParse(lineNumberText, out lineNumber))
                            {
                            }
                        }
                        var r = lineNumber;
                        Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory:@ line number: " + lineNumber + " " + ex.Message, 9);
                        //Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory: " + ex.Message, 9);
                    }
                }
                return listFiles;
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                //to find the exception line number
                var lineNumber = 0;
                const string lineSearch = ":line ";
                var index = ex.StackTrace.LastIndexOf(lineSearch);
                if (index != -1)
                {
                    var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                    if (int.TryParse(lineNumberText, out lineNumber))
                    {
                    }
                }
                var r = lineNumber;
                Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory:@ line number: " + lineNumber + " " + ex.Message, 9);

                labelEvent.Set();
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("The specified Data path, file name, or both are too long..!\nPlease select proper path for Loading the Images..", "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return listFiles;

            }

            catch (Exception ex)
            {
                //to find the exception line number
                var lineNumber = 0;
                const string lineSearch = ":line ";
                var index = ex.StackTrace.LastIndexOf(lineSearch);
                if (index != -1)
                {
                    var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                    if (int.TryParse(lineNumberText, out lineNumber))
                    {
                    }
                }
                var r = lineNumber;
                Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory:@ line number: " + lineNumber + " " + ex.Message, 9);
                //Utilities.LogMessage("MainWindow::GetAllFilesFromDirectory: " + ex.Message, 9);
                return listFiles;
            }
        }

        /// <summary>
        /// Function to Load CSV file header column names to string of arrays
        /// </summary>
        public string[] GetColumnHeaderNames(List<string> listCSVLines)
        {
            string[] targetold = { "file_size", "file_attributes", "region_id" };
            string[] linetoSkip = { "Date", "CSV Datasheet" };//the name of the column to skip

            int cnt = 0;
            string[] splitHeader = null;
            for (int index = 0; index < listCSVLines.Count; index++)
            {
                if (cnt > 4)
                    break;

                if (string.IsNullOrWhiteSpace(listCSVLines[index]) || linetoSkip.ToList().Exists(temp => listCSVLines[index].Contains(temp)))
                {
                    cnt++;
                    continue;
                }

                if (listCSVLines[index].Split(',').Count() > 0 || listCSVLines[index].Contains("file") || listCSVLines[index].Contains("Image"))
                    splitHeader = listCSVLines[index].Split(',').Where(item => !targetold.Contains(item)).Select(s => s.Replace("#", "")).ToArray();

                break;
            }

            if (splitHeader == null)
                return splitHeader = new string[] { "filename","region_count","region_shape_attributes","region_attributes","to_be_corrected","line_name"};
            else
            {
                //Replace linename to line_name
                if (Array.Exists(splitHeader, d => d.Contains("linename")))
                    splitHeader[Array.IndexOf(splitHeader, splitHeader.Where(s => s.Contains("linename")).FirstOrDefault())] = "line_name";
                return splitHeader;
            }
        }

        /// <summary>
        /// Function to Get Class statistics from loded Datasheet
        /// </summary>
        public List<ClassStats> GetClassFolderStatistics(string[] arrSelDatasheets = null)
        {
            List<ClassStats>  listClassStat = new List<ClassStats>();
            List<ClassFolderStat> tempClassFolderStat = new List<ClassFolderStat>();
            if (arrSelDatasheets == null || arrSelDatasheets.Length == 0)
                tempClassFolderStat = ListClassFolderStat;
            else
                tempClassFolderStat = ListClassFolderStat.Where(temp => arrSelDatasheets.Contains(temp.ImportDatasheetName)).ToList();

            var tempClassStatList = (from tempList in tempClassFolderStat
                                     group tempList by new { tempList.ClassAliasName } into tempGroup
                                     select new
                                     {
                                         tempGroup.Key.ClassAliasName,
                                         ClassCount = tempGroup.Sum(t => t.ClassCount),
                                         SingleSpotCount = tempGroup.Sum(t => t.SingleSpotCount),
                                         PhaseContrastCount = tempGroup.Sum(t => t.PhaseContrastCount),
                                     }).ToList();

            foreach (var tempItem in settings.dictEVSupervisorClass)
            {
                string strAlias = tempItem.Value.Split('(', ')').Length > 1 ? tempItem.Value.Split('(', ')')[1] : "";
                var curFolderstat = tempClassStatList.FirstOrDefault(item => item.ClassAliasName.ToUpper() == strAlias.ToUpper());
                ClassStats curClassStats = new ClassStats();
                listClassStat.Add(curClassStats);

                curClassStats.ClassName = tempItem.Value.Split('(', ')').Length > 0 ? tempItem.Value.Split('(', ')')[0] : "";
                curClassStats.AliasName = strAlias;
                curClassStats.ClassID = tempItem.Key.ToString();

                var tempModifiedClass = ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == strAlias.ToUpper());
                curClassStats.CurrentClassID = tempModifiedClass != null? tempModifiedClass.ModifiedID : "";
                curClassStats.ModifiedID = "";
                if (curFolderstat != null)
                {
                    curClassStats.Count = curFolderstat.ClassCount;
                    curClassStats.SingleSpotCount = curFolderstat.SingleSpotCount;
                    curClassStats.PhaseContrastCount = curFolderstat.PhaseContrastCount;
                    
                }
                else
                {
                    curClassStats.Count = 0;
                    curClassStats.SingleSpotCount = 0;
                    curClassStats.PhaseContrastCount = 0;
                }
            }

            string[] arrayAlias = settings.dictEVSupervisorClass.Values.Select(temp => temp.Split('(', ')').Length > 1 ? temp.Split('(', ')')[1].ToUpper() : "").ToArray();
            var listClass = tempClassStatList.Where(item => !arrayAlias.Contains(item.ClassAliasName.ToUpper())).ToList();

            foreach (var curClass in listClass)
            {
                var tempModifiedClass = ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == curClass.ClassAliasName.ToUpper());
                listClassStat.Add(new ClassStats
                {
                    ClassName = "Unknown Class",
                    AliasName = curClass.ClassAliasName,
                    Count = curClass.ClassCount,
                    SingleSpotCount = curClass.SingleSpotCount,
                    PhaseContrastCount = curClass.PhaseContrastCount,
                    ClassID = "",
                    CurrentClassID = tempModifiedClass != null ? tempModifiedClass.ModifiedID : "",
                    ModifiedID = ""
                });
            }

            return listClassStat;
        }

        /// <summary>
        /// Function to Get Class Format List
        /// </summary>
        public List<ClassFormat> GetClassFormatList(string[] arrSelDatasheets = null)
        {
            List<ClassFormat>  listClassFormat = new List<ClassFormat>();
            List<ClassFolderStat> tempClassFolderStat = new List<ClassFolderStat>();
            if (arrSelDatasheets == null || arrSelDatasheets.Length == 0)
                tempClassFolderStat = ListClassFolderStat;
            else
                tempClassFolderStat = ListClassFolderStat.Where(temp => arrSelDatasheets.Contains(temp.ImportDatasheetName)).ToList();

            var tempClassStatList = (from tempList in tempClassFolderStat
                                     group tempList by new { tempList.ClassAliasName } into tempGroup
                                     select new
                                     {
                                         tempGroup.Key.ClassAliasName,
                                         ClassCount = tempGroup.Sum(t => t.ClassCount),
                                         SingleSpotCount = tempGroup.Sum(t => t.SingleSpotCount),
                                         PhaseContrastCount = tempGroup.Sum(t => t.PhaseContrastCount),
                                     }).ToList();

            foreach (var curClass in settings.dictEVSupervisorClass)
            {
                string strAlias = curClass.Value.Split('(', ')').Length > 1 ? curClass.Value.Split('(', ')')[1] : "";
                var curFolderstat = tempClassStatList.FirstOrDefault(item => item.ClassAliasName.ToUpper() == strAlias.ToUpper());

                ClassFormat curClassFormat = new ClassFormat();
                listClassFormat.Add(curClassFormat);

                curClassFormat.ClassName = curClass.Value;
                curClassFormat.Alias = strAlias;
                curClassFormat.Option = EnumImageType.Default;
                curClassFormat.VisibilityExportField = settings.dictProjectList[settings.CurrentProject].Contains("LS3 BV")? Visibility.Visible : Visibility.Collapsed;
                curClassFormat.ColumnWidthExportField = settings.dictProjectList[settings.CurrentProject].Contains("LS3 BV")? "0.15*" : "auto";
                if (curFolderstat != null){
                    curClassFormat.IsClassEnable = curFolderstat.ClassCount > 0 ? true : false;
                    curClassFormat.ClassCount = curFolderstat.ClassCount;
                    curClassFormat.ModifiedCount = curFolderstat.ClassCount;
                    curClassFormat.PhaseContrastCount = curFolderstat.PhaseContrastCount;
                    curClassFormat.SingleSpotCount = curFolderstat.SingleSpotCount;
                    curClassFormat.ExportSinglespot = curFolderstat.SingleSpotCount;
                    curClassFormat.ExportPhaseContrast = curFolderstat.PhaseContrastCount;
                }
                else{
                    curClassFormat.IsClassEnable = false;
                    curClassFormat.ClassCount = 0;
                    curClassFormat.ModifiedCount = 0;
                    curClassFormat.PhaseContrastCount = 0;
                    curClassFormat.SingleSpotCount = 0;
                    curClassFormat.ExportSinglespot = 0;
                    curClassFormat.ExportPhaseContrast = 0;                        
                }
            }

            string[] arrayAlias = settings.dictEVSupervisorClass.Values.Select(temp => temp.Split('(', ')').Length > 1 ? temp.Split('(', ')')[1].ToUpper() : "").ToArray();
            var listClass = tempClassStatList.Where(item => !arrayAlias.Contains(item.ClassAliasName.ToUpper())).ToList();

            foreach (var curClass in listClass){
                listClassFormat.Add(new ClassFormat
                {
                    ClassName = curClass.ClassAliasName,
                    Alias = curClass.ClassAliasName,
                    Option = EnumImageType.Default,
                    IsClassEnable = curClass.ClassCount > 0 ? true : false,
                    ClassCount = curClass.ClassCount,
                    ModifiedCount = curClass.ClassCount,
                    PhaseContrastCount = curClass.PhaseContrastCount,
                    SingleSpotCount = curClass.SingleSpotCount,
                    ExportSinglespot = curClass.SingleSpotCount,
                    ExportPhaseContrast = curClass.PhaseContrastCount,
                    VisibilityExportField = settings.CurrentProject != "P1" ? Visibility.Collapsed : Visibility.Visible,
                    ColumnWidthExportField = settings.CurrentProject != "P1" ? "auto" : "0.15*"
                });
            }

            return listClassFormat;
        }

        public void UpdateAugmentationClassList()
        {
            List<ClassStats> listClassStats = GetClassFolderStatistics();
            ListAugmentTypeClass = new List<AugmentTypeClass>();
            foreach (ClassStats curClassStat in listClassStats) 
                ListAugmentTypeClass.Add(new AugmentTypeClass(curClassStat));
                
            if (ListAugmentTypeClass.Count > 0)
                IsVisibleAgmentButton = Visibility.Visible;

            this.Dispatcher.Invoke(() => {
                ListAugmentationView.ItemsSource = ListAugmentTypeClass;
                ListAugmentationView.Items.Refresh();
                NotifyPropertyChanged("SourceTotalCount");
            });
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of loading images
        /// </summary>
        private void bgwDowork_LoadImages(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadLoadImageFolder = new Thread(LoadImageFiles);
                threadLoadImageFolder.IsBackground = true;
                threadLoadImageFolder.Priority = ThreadPriority.Highest;
                threadLoadImageFolder.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of loading saved work 
        /// </summary>
        private void bgwDowork_LoadProcessedImageStat(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadLoad = new Thread(LoadProcessedImageStats);
                threadLoad.IsBackground = true;
                threadLoad.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of saving work in application stats path
        /// </summary>
        private void bgwDowork_SaveProcessedImageStat(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threaSaveStat = new Thread(() => SaveProcessedImageStats());
                threaSaveStat.IsBackground = true;
                threaSaveStat.Priority = ThreadPriority.Lowest;
                threaSaveStat.Start();
            }
        }

        private void bgwDowork_SaveLabelledWorkStat(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threaSaveStat = new Thread(() => SaveLabelledWorkStats());
                threaSaveStat.IsBackground = true;
                threaSaveStat.Priority = ThreadPriority.Lowest;
                threaSaveStat.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of exporting csv file
        /// </summary>
        private void bgwDowork_CSVFileExportData(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] args = e.Argument as object[];
                Thread threadCSVExport = new Thread(() => SaveCSVFileExportData(args));
                threadCSVExport.IsBackground = true;
                threadCSVExport.Priority = ThreadPriority.Lowest;
                threadCSVExport.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of exporting csv file
        /// </summary>
        private void bgwDowork_RemoveMultipleOverlays(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadMultiOverlay = new Thread(RemoveMultipleOverlays);
                threadMultiOverlay.IsBackground = true;
                threadMultiOverlay.Priority = ThreadPriority.Lowest;
                threadMultiOverlay.Start();
            }
        }

        private void bgwDowork_ExportSegregatedImages(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadProcess = new Thread(ExportSegregatedImagesToDisk);
                threadProcess.IsBackground = true;
                threadProcess.Priority = ThreadPriority.Lowest;
                threadProcess.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of exporting json files
        /// </summary>
        private void bgwDowork_JSONFileExport(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arg = e.Argument as object[];
                Thread threadJSONExport = new Thread(() => SaveJsonFileExportData(arg));
                threadJSONExport.IsBackground = true;
                threadJSONExport.Priority = ThreadPriority.Lowest;
                threadJSONExport.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of saving all labelled ROI into output folder
        /// </summary>
        public void bgwDowork_AllROIExport(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arrArgs = e.Argument as object[];
                Thread threadROIExport = new Thread(() => SaveAllROItoDisk(arrArgs));
                threadROIExport.IsBackground = true;
                threadROIExport.Priority = ThreadPriority.Lowest;
                threadROIExport.Start();
            }
        }

        public void bgwDowork_RetrieveValidatedImages(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arrArgs = e.Argument as object[];
                Thread threadRetrieveImages = new Thread(() => RetrieveValidatedImages(arrArgs));
                threadRetrieveImages.IsBackground = true;
                threadRetrieveImages.Priority = ThreadPriority.Lowest;
                threadRetrieveImages.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of xml export
        /// </summary>
        public void bgwDowork_ExportasXML(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadXMLExport = new Thread(ExportXMLDataintoOutput);
                threadXMLExport.IsBackground = true;
                threadXMLExport.Priority = ThreadPriority.Lowest;
                threadXMLExport.Start();
            }
        }

        public void bgwDowork_ImageAugmentationProcess(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arrArgs = e.Argument as object[];
                Thread threadAugmentation = new Thread(() => ImageAugmentationProcess(arrArgs));
                threadAugmentation.IsBackground = true;
                threadAugmentation.Priority = ThreadPriority.Lowest;
                threadAugmentation.Start();
            }
        }

        public void bgwDowork_AutoLabellerProcess(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadAutoLabeller = new Thread(AutoLabellerProcess);
                threadAutoLabeller.IsBackground = true;
                threadAutoLabeller.Priority = ThreadPriority.Lowest;
                threadAutoLabeller.Start();
            }
        }

        /// <summary>
        /// Function of call BackGround worker thread to start process of filetrng of class index changed
        /// </summary>
        private void bgwDowork_cmbClassFilter_SelectionChanged(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threaClassFilter = new Thread(LoadClassFilteredImages);
                threaClassFilter.IsBackground = true;
                threaClassFilter.Priority = ThreadPriority.Lowest;
                threaClassFilter.Start();
            }
        }

        private void bgwDowork_LoadPredictionText(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threaClassFilter = new Thread(LoadPredictionMetaDataFromText);
                threaClassFilter.IsBackground = true;
                threaClassFilter.Priority = ThreadPriority.Lowest;
                threaClassFilter.Start();
            }
        }

        private void bgwDowork_ImageAnalysis(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arg = e.Argument as object[];
                Thread threadImageAnalysis = new Thread(() => ImageAnalysisProcess(arg));
                threadImageAnalysis.IsBackground = true;
                threadImageAnalysis.Priority = ThreadPriority.Lowest;
                threadImageAnalysis.Start();
            }
        }

        private void bgwDowork_DisplayImageAnalysis(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                string arg = e.Argument as string;
                Thread threadImageAnalysis = new Thread(() => ImageAnalysisDisplay(arg));
                threadImageAnalysis.IsBackground = true;
                threadImageAnalysis.Priority = ThreadPriority.Lowest;
                threadImageAnalysis.Start();
            }
        }

        private void bgwDowork_ImageFormatChange(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                string arg = e.Argument as string;
                Thread threadFormatImage = new Thread(() => FormatImageAndExport(arg));
                threadFormatImage.IsBackground = true;
                threadFormatImage.Priority = ThreadPriority.Lowest;
                threadFormatImage.Start();
            }
        }

        private void bgwDowork_LoadSegregatedDatasheet(object sender, DoWorkEventArgs e)
        {
            if (bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                bool isLoaded = false;
                Thread threadProcess = new Thread(() => isLoaded = LoadSegregatedImagesFromCSV());
                threadProcess.IsBackground = true;
                threadProcess.Priority = ThreadPriority.Lowest;
                threadProcess.Start();
            }
        }

        public void bgwProgressChange_Load(object sender, ProgressChangedEventArgs e)
        {
            if (progressBar != null)
            {
                if (e.ProgressPercentage == -1)
                    progressBar.pbStatus.Maximum = Convert.ToInt32(e.UserState);
                else
                    progressBar.pbStatus.Value = e.ProgressPercentage;
            }
        }

        public void OnWorkerMethodStart(string statusText = "Load")
        {
            progressBar = new ProgressBarWindow();
            progressBar.pbStatus.IsIndeterminate = true;
            if(statusText == "Load")
                progressBar.pbStausText.Text = "Loading Please Wait...";
            else
                progressBar.pbStausText.Text = "Please wait while Saving...";
            progressBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            progressBar.Owner = this;
            progressBar.ShowDialog();
        }

        public void OnWorkerMethodStart_withPercentage()
        {
            progressBar = new ProgressBarWindow();
            progressBar.pbStatus.IsIndeterminate = false;
            progressBar.pbStausText.Text = "Processing..";
            progressBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            progressBar.Owner = this;
            progressBar.ShowDialog();
        }

        public void OnWorkerMethodStart_LoadFile(Window app, string progressText = "Processing Please wait...")
        {
            progressBar = new ProgressBarWindow();
            progressBar.pbStatus.IsIndeterminate = true;
            progressBar.pbStausText.Text = progressText;
            progressBar.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progressBar.Owner = app;
            progressBar.ShowDialog();
        }

        public void OnWorkerMethodStartWithPercent_ProcessFile(Window app, string progressText = "Loading Please Wait...")
        {
            progressBar = new ProgressBarWindow();
            progressBar.pbStatus.IsIndeterminate = false;
            progressBar.pbStausText.Text = progressText;
            progressBar.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progressBar.Owner = app;
            progressBar.ShowDialog();
        }

        /// <summary>
        /// Function to close the progress bar window after completion of background worker process
        /// </summary>
        public void OnWorkerMethodComplete(string message)
        {
            if (progressBar != null)
            {
                progressBar.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    progressBar.Close();
                }));
            }
        }

        /// <summary>
        /// Function to clear the application generated files/ folders
        /// </summary>
        public void ClearStatTempFiles()
        {
            string strDataPath = settings.StatsFilePath + @"Temp Files";
            if (System.IO.Directory.Exists(strDataPath))
                System.IO.Directory.Delete(strDataPath,true);
        }

        /// <summary>
        /// Function to Show no project selected message when project was not selected in settings window
        /// </summary>
        public bool ShowMessageNoProject(object sender)
        {
            if (settings.CurrentProject == "P0")
            {
                System.Windows.MessageBox.Show("No Project Selected..! Please select project from File->Setting menu..", "No Project", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Project not selected in settings page.");
                return true;
            }
            else if (sender != null && sender.GetType().Name == "RadRibbonButton" && (((sender as Telerik.Windows.Controls.RadRibbonButton).Parent as Telerik.Windows.Controls.RadRibbonGroup).IsEnabled == false
                     || ((sender as Telerik.Windows.Controls.RadRibbonButton).Parent as Telerik.Windows.Controls.RadRibbonGroup).Visibility == Visibility.Collapsed))
                return true;

            return false;
        }

        /// <summary>
        /// Function to Check type of Files loaded and give warning message
        /// </summary>
        public bool CheckCSVFileLoaded(string fileType = ".csv")
        {
            if (settings.ImportFilePath == null || settings.ImportFilePath.Length == 0 || !settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == fileType))
            {
                if(fileType == ".csv")
                    System.Windows.MessageBox.Show("CSV File not found..!\nPlease Import from File Menu->Import CSV.", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                else if(fileType == ".json")
                    System.Windows.MessageBox.Show("JSON File not found..!\nPlease Import from File Menu->Import JSON.", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                else if (fileType == ".xml")
                    System.Windows.MessageBox.Show("XML File not found..!\nPlease Import from File Menu->Import XML.", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);

                Utilities.LogMessage(fileType + " file not found in path " + settings.ImportFilePath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Function to Check the loaded datasheet file accessing permission. If not display the warning message 
        /// </summary>
        public bool CheckFileAccessToFormat()
        {
            if (settings.ImportFilePath == null)
                return false;
            for (int i = 0; i < settings.ImportFilePath.Length; i++)
            {
                if (settings.CheckFileAccess(settings.ImportFilePath[i]))
                {
                    System.Windows.MessageBox.Show("Some file cannot be accessible\nMake sure the file is not accessed by other application.", "Access Denied", MessageBoxButton.OK,
                        MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return false;
                }
            }
            return true;
        }
    }

    public class DataViolation
    {
        public string ImagePathName { get; set; }

        public string ImageFileName { get; set; }

        public int ViolatedRow { get; set; }

        public bool FilenameViolated { get; set; }

        public bool RegionCountViolated { get; set; }

        public bool ShapeViolated { get; set; }

        public bool RegionClassViolated { get; set; }

        public DataViolation(string ImageFileName, string ImagePathName, int rowNum)
        {
            this.ImageFileName = ImageFileName;
            this.ImagePathName = ImagePathName;
            ViolatedRow = rowNum;
        }
    }

    [Serializable]
    public class ClassFolderStat
    {
        public string ImportDatasheetName { get; set; }

        public string ClassFolderName { get; set; }

        public string ClassAliasName { get; set; }

        public string ClassID { get; set; }

        public int ClassCount { get; set; }

        public int SingleSpotCount { get; set; }

        public int PhaseContrastCount { get; set; }
    }

    public class ImportDatasheetData
    {
        public string DatasheetName { get; set; }

        public List<string[]> ListImportData { get; set; }

        public ImportDatasheetData(string dataSheetName)
        {
            ListImportData = new List<string[]>();
            DatasheetName = dataSheetName;
        }
    }

    public class ModifiedClass
    {
        public string ModifiedID { get; set; }
        public string ModifiedClassName { get; set; }

        public ModifiedClass(string ID, string ClassAlias)
        {
            ModifiedID = ID;
            ModifiedClassName = ClassAlias;
        }
    }
}
