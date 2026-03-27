using System;
using System.Collections.Generic;
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
using MoreLinq;
using System.ComponentModel;
using System.Threading;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for DatasheetSplitterWindow.xaml
    /// </summary>
    public partial class DatasheetSplitterWindow : Window
    {
        MainWindow app;
        List<string> listClass = new List<string>();
        List<string> listLines = new List<string>();
        List<string> ListFileteredRecord;
        List<ClassFormat> ListClassFormat = new List<ClassFormat>();
        List<LineFormat> ListLineFormat = new List<LineFormat>();
        string[] arrColumnHeader;
        bool bIsPairedRecords = false;
        BackgroundWorker BGWorkerFormat;
        int[] splitPercent = new int[3];
        int currentTabIndex = 0;
        string[] arrSelDatasheet;
        bool bIsCheckPair;

        public DatasheetSplitterWindow(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            InitializeCheckBox();
            InitialiZeControls();
            DataContext = this;
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

        private void InitialiZeControls()
        {
            radTwo.Checked += radSplit_Checked;
            radTwo.IsChecked = true;
            radThree.IsChecked = false;
            txtSplit1.Text = string.Empty;
            txtSplit2.Text = string.Empty;
            txtSplit3.Text = string.Empty;
            lblRecordCount.Content = "0";
            chkPair.IsChecked = true;
            bIsCheckPair = app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV")? true : false;
            spShufflePair.Visibility = app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV")? Visibility.Visible : Visibility.Collapsed;
            gpLine.Visibility = ListLineFormat.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            chkSelAllLine.IsChecked = ListLineFormat.Exists(temp => !temp.IsLineSelected) ? false : true; 
            chkSelAllClass.IsChecked = ListClassFormat.Exists(temp => !temp.IsClassEnable) ? false : true;
        }

        private void InitializeCheckBox()
        {
            ListClassFormat = app.GetClassFormatList();
            ListClassView.ItemsSource = ListClassFormat;

            for (int i = 0; i < app.settings.LineList.Length; i++)
            {
                ListLineFormat.Add(new LineFormat
                {
                    LineName = app.settings.LineList[i].ToString(),
                    IsLineSelected = true
                });
            }
            ListLineView.ItemsSource = ListLineFormat;
        }

        List<string> fileList = new List<string>();
        public List<string> LoadedFileList
        {
            get
            {
                if (app.settings.ImportFilePath == null || app.settings.ImportFilePath.Length == 0)
                    fileList.Add("No DataSheet Loaded");
                else
                    fileList = app.settings.ImportFilePath.ToList();

                return fileList;
            }
        }

        public double SetMaxWidth
        {
            get
            {
                return SystemParameters.PrimaryScreenWidth - 250;
            }
        }

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void btnSelectNext_Click(object sender, RoutedEventArgs e)
        {
            if (!app.CheckFileAccessToFormat())
                return;

            InitialiZeControls();
            listClass = ListClassFormat.Where(item => item.IsClassEnable == true).Select(temp => temp.Alias.ToUpper()).ToList();
            listLines = ListLineFormat.Where(item => item.IsLineSelected == true).Select(temp => temp.LineName).ToList();
            ListFileteredRecord = new List<string>();
            if (listClass.Count == 0 && listLines.Count == 0){
                MessageBoxResult result = MessageBox.Show("Do you wish to continue without selecting Class/Lines?\nIf No Select Class/Lines to filter datasheet", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
                       
            Mouse.OverrideCursor = Cursors.Wait;
            popUpWait.IsOpen = true;

            string strDataPath = app.settings.StatsFilePath + @"Temp Files";
            string[] tempFiles = null;
            if (System.IO.Directory.Exists(strDataPath))
                tempFiles = Directory.GetFiles(strDataPath);

            //DatasheetLoaded = tempFiles != null && tempFiles.Length > 0 ? tempFiles : app.settings.ImportFilePath;
            arrColumnHeader = app.DictColHeaders.Values.Aggregate((max, cur) => max.Length > cur.Length ? max : cur);
            GetFilteredRecordsFromDataSheet();

            if (bIsCheckPair)
                bIsPairedRecords = GetPairStatusOfFilteredRecords();
            lblRecordCount.Content = ListFileteredRecord.Count.ToString();
            tabSplit.SelectionChanged -= tabSplit_SelectionChanged;
            tabSplit.SelectedIndex = 1;
            currentTabIndex = 1;
            tabSplit.SelectionChanged += tabSplit_SelectionChanged;
            lblDatasheetName.Text = arrSelDatasheet == null || arrSelDatasheet.Length == 0 ? "All Datasheet" : string.Join(",  ", arrSelDatasheet);
            Mouse.OverrideCursor = null;
            popUpWait.IsOpen = false;           
        }

        private void GetFilteredRecordsFromDataSheet()
        {
            List<ImportDatasheetData> tempListDataSheetLoaded = new List<ImportDatasheetData>();
            foreach (ImportDatasheetData temp in app.ListDatasheetImportData){
                List<string[]> tempArray = new List<string[]>(temp.ListImportData);
                if (arrSelDatasheet == null || arrSelDatasheet.Length == 0)
                    tempListDataSheetLoaded.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                else if (arrSelDatasheet.Length == 1 && arrSelDatasheet.Contains(temp.DatasheetName)){
                    tempListDataSheetLoaded.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
                    break;
                }
                else if(arrSelDatasheet.Length > 1 && arrSelDatasheet.Contains(temp.DatasheetName))
                    tempListDataSheetLoaded.Add(new ImportDatasheetData(temp.DatasheetName) { ListImportData = tempArray });
            }

            for (int index = 0; index < tempListDataSheetLoaded.Count; index++){
                List<string> listCSVLines = GetFilteredClassOrLineData(tempListDataSheetLoaded[index].ListImportData);
                ListFileteredRecord.AddRange(listCSVLines);
            }
        }

        private List<string> GetFilteredClassOrLineData(List<string[]> listCSVLines)
        {
            for(int i = 0; i < listCSVLines.Count; i++)
            {
                string[] lineSplit = listCSVLines[i].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                if (lineSplit.Length > 3)
                {
                    if (!app.IsValidCSVLine(lineSplit)){
                        listCSVLines.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (listClass.Count == ListClassFormat.Count && listLines.Count == ListLineFormat.Count)
                        continue;
                    //string tempID = Regex.Match(lineSplit[3], @"\b[:]\s*[0-9]+").ToString().Replace(":", "").Trim();
                    string tempClass = Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                    string lineName = string.Empty;
                    if (Array.Exists(arrColumnHeader, d => d.Contains("line")))
                    {
                        if (arrColumnHeader.Contains("to_be_corrected"))
                            lineName = lineSplit.Length > 5 ? lineSplit[5].Replace(" ", "").ToUpper() : "";
                        else
                            lineName = lineSplit.Length > 4 ? lineSplit[4].Replace(" ", "").ToUpper() : "";
                    }
                    
                    if(listClass.Count < ListClassFormat.Count && listLines.Count < ListLineFormat.Count)
                    {
                        if (!listClass.Contains(tempClass) || (lineName != "" && !listLines.ConvertAll(d => d.Replace(" ","")).Contains(lineName))){
                            listCSVLines.RemoveAt(i);
                            i--;
                        }
                    }
                    else if(listClass.Count < ListClassFormat.Count && listLines.Count == ListLineFormat.Count)
                    {
                        if(!listClass.Contains(tempClass)){
                            listCSVLines.RemoveAt(i);
                            i--;
                        }
                    }
                    else if(listClass.Count == ListClassFormat.Count && listLines.Count < ListLineFormat.Count)
                    {
                        if (lineName != "" && !listLines.ConvertAll(d => d.Replace(" ", "")).Contains(lineName)){
                            listCSVLines.RemoveAt(i);
                            i--;
                        }
                    }                    
                }
            }
            return listCSVLines.Select(temp => string.Join(",", temp)).ToList();
        }

        private bool GetPairStatusOfFilteredRecords()
        {
            List<string> tempFilteredRecords = new List<string>(ListFileteredRecord);
            for(int i = 0; i < tempFilteredRecords.Count; i++)
            {
                string strImageName = Regex.Split(tempFilteredRecords[i], @"(?<!,[^[]+\{[^}]+),").FirstOrDefault();
                if(!string.IsNullOrEmpty(strImageName))
                {
                    int index = GetIndexOfImagePairWithorWithoutClass(tempFilteredRecords, strImageName);
                    if (index == -1)
                        return false;
                    else
                    {
                        tempFilteredRecords.RemoveAt(i);
                        index--;
                        tempFilteredRecords.RemoveAt(index);
                        i--;
                    }
                }
            }
            return true;
        }

        private void radSplit_Checked(object sender, RoutedEventArgs e)
        {
            txtSplit1.Text = string.Empty;
            txtSplit2.Text = string.Empty;
            txtSplit3.Text = string.Empty;
            if ((sender as RadioButton).Name == "radTwo")
            {
                spSplit3.Visibility = Visibility.Collapsed;
                txtSplit2.IsEnabled = false;
            }
            else if ((sender as RadioButton).Name == "radThree")
            {
                spSplit3.Visibility = Visibility.Visible;
                txtSplit2.IsEnabled = true;
                txtSplit3.IsEnabled = false;
            }
        }

        private void chkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).Name == "chkSelAllClass")
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

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            tabSplit.SelectionChanged -= tabSplit_SelectionChanged;
            tabSplit.SelectedIndex = 0;
            currentTabIndex = 0;
            tabSplit.SelectionChanged += tabSplit_SelectionChanged;
        }

        private void btnProceed_Click(object sender, RoutedEventArgs e)
        {
            if (ListFileteredRecord == null || ListFileteredRecord.Count < 4){
                MessageBox.Show("To split datasheet minimum record should be 4", "Split Failed", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            if (bIsCheckPair && !bIsPairedRecords && chkPair.IsChecked.Value)
                MessageBox.Show("Records are not in pair..!\nClick Ok to continue..", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            splitPercent[0] = txtSplit1.Text == string.Empty ? 0 : Convert.ToInt16(txtSplit1.Text);
            splitPercent[1] = txtSplit2.Text == string.Empty ? 0 : Convert.ToInt16(txtSplit2.Text);
            splitPercent[2] = txtSplit3.Text == string.Empty ? 0 : Convert.ToInt16(txtSplit3.Text);

            if ((radTwo.IsChecked.Value && (splitPercent[0] == 0 || splitPercent[1] == 0)) || (radThree.IsChecked.Value && (splitPercent[0] == 0 || splitPercent[1] == 0 || splitPercent[2] == 0)))
            {
                MessageBox.Show("Split % cannot be blank or zero", "Invalid", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            BGWorkerFormat.DoWork += bgwDowork_DataSheetSplit;
            BGWorkerFormat.ProgressChanged += app.bgwProgressChange_Load;
            BGWorkerFormat.RunWorkerAsync();
            app.OnWorkerMethodStartWithPercent_ProcessFile(this, "Processing Please wait...");               
        }

        private void bgwDowork_DataSheetSplit(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadSplit = new Thread(SplitAndShuffleCSVDatasheet);
                threadSplit.IsBackground = true;
                threadSplit.Start();
            }
        }

        private void SplitAndShuffleCSVDatasheet()
        {
            try
            {
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
                List<string> listTempFilteredRecords = new List<string>(ListFileteredRecord);
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Data Split";

                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                bool bIsTwoFile = false, bIsThreeFile = false, bIsCheckPair = false;

                this.Dispatcher.Invoke(() =>
                {
                    bIsTwoFile = radTwo.IsChecked.Value;
                    bIsThreeFile = radThree.IsChecked.Value;
                    bIsCheckPair = chkPair.IsChecked.Value;
                });

                StringBuilder sb = new StringBuilder();
                int splitCnt = bIsTwoFile ? 2 : 3;
                long[] nSplitCount = new long[splitCnt];
                List<string> listSplitRecords = new List<string>();
                string strDateTimeStamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                
                for (int i = 0; i < splitCnt; i++)
                {
                    nSplitCount[i] = (i != splitCnt - 1) ? Convert.ToInt64(Math.Round(Convert.ToDouble(ListFileteredRecord.Count * splitPercent[i]) / 100, 0)) :
                                       ListFileteredRecord.Count - nSplitCount.Sum();

                    if (bIsCheckPair)
                    {
                        listSplitRecords = GetSplitDatasheetRecordsWithPair(listTempFilteredRecords, nSplitCount[i]);
                        nSplitCount[i] = listSplitRecords.Count;
                    }
                    else
                        listSplitRecords = GetSplitDatasheetRecordsWithoutPair(listTempFilteredRecords, nSplitCount[i]);

                    if (listSplitRecords.Count == 0)
                    {
                        MessageBoxResult result = MessageBox.Show("No records found in File " + (i + 1) + "..\nDo you wish to export?", "No Records", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                            continue;
                    }
                    sb.Clear();
                    sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                    string colHeader = string.Join(",", arrColumnHeader);
                    string strSplitCSVPath = System.IO.Path.Combine(strDataPath, "data_" + strDateTimeStamp + "_file" + (i + 1).ToString() + ".csv");

                    File.WriteAllText(strSplitCSVPath, sb.AppendLine(colHeader).ToString());
                    File.AppendAllLines(strSplitCSVPath, listSplitRecords);
                    if (i == splitCnt - 1 && listTempFilteredRecords.Count > 0)
                    {
                        File.AppendAllLines(strSplitCSVPath, listTempFilteredRecords);
                        nSplitCount[i] = listSplitRecords.Count + listTempFilteredRecords.Count;
                    }
                }

                string strMessage = string.Empty;
                if (bIsTwoFile)
                    strMessage = "File 1 Records : " + nSplitCount[0] + "\nFile 2 Records : " + nSplitCount[1];
                else if (bIsThreeFile)
                    strMessage = "File 1 Records : " + nSplitCount[0] + "\nFile 2 Records : " + nSplitCount[1] + "\nFile 3 Records : " + nSplitCount[2];

                app.OnWorkerMethodComplete("complete");
                MessageBox.Show(strMessage, "Split and Export", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Dispatcher.Invoke(() => this.Close());
            }

            catch (Exception ex) when(ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("Something went wrong..!\nDatasheet Could not Split and Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("DatasheetSplitter::Exportandsplit_Click: " + ex.Message, 0);
            }
        }

        private List<string> GetSplitDatasheetRecordsWithPair(List<string> listTempFilteredRecords, long nSPlitCount)
        {
            List<string> listSplitRecords = new List<string>();
            Random random = new Random();
            long k = 0;
            Dispatcher.Invoke(() => app.progressBar.pbStatus.Maximum = nSPlitCount);

            for (int index = 0; index < nSPlitCount; index++)
            {
                if (listTempFilteredRecords.Count < 1)
                    continue;
                int rndIndex = random.Next(0, listTempFilteredRecords.Count - 1);
                listSplitRecords.Add(listTempFilteredRecords[rndIndex]);
                string strImageName = Regex.Split(listTempFilteredRecords[rndIndex], @"(?<!,[^[]+\{[^}]+),").FirstOrDefault();
                string strClass = Regex.Split(listTempFilteredRecords[rndIndex], @"(?<!,[^[]+\{[^}]+),").Count() > 3? Regex.Split(listTempFilteredRecords[rndIndex], @"(?<!,[^[]+\{[^}]+),")[3] : "";
                
                listTempFilteredRecords.RemoveAt(rndIndex);
                if(!string.IsNullOrEmpty(strImageName))
                {
                    int delIndex = GetIndexOfImagePairWithorWithoutClass(listTempFilteredRecords, strImageName, strClass);

                    if (delIndex != -1)
                    {
                        if (Regex.Split(listSplitRecords.Last(), @"(?<!,[^[]+\{[^}]+),").FirstOrDefault().Split('.')[1] == "h")
                            listSplitRecords.Insert(listSplitRecords.IndexOf(listSplitRecords.Last()), listTempFilteredRecords[delIndex]);
                        else
                            listSplitRecords.Add(listTempFilteredRecords[delIndex]);

                        listTempFilteredRecords.RemoveAt(delIndex);
                        k = nSPlitCount--;
                        Dispatcher.Invoke(() =>  app.progressBar.pbStatus.Maximum = k);
                    }
                }
                Dispatcher.Invoke(() => app.progressBar.pbStatus.Value = index);
            }
            return listSplitRecords;
        }

        private List<string> GetSplitDatasheetRecordsWithoutPair(List<string> listTempFilteredRecords, long nSPlitCount)
        {
            List<string> listSplitRecords = new List<string>();
            Random random = new Random();
            Dispatcher.Invoke(() => app.progressBar.pbStatus.Maximum = nSPlitCount );

            for (int index = 0; index < nSPlitCount; index++)
            {
                int rndIndex = random.Next(0, listTempFilteredRecords.Count - 1);
                listSplitRecords.Add(listTempFilteredRecords[rndIndex]);
                listTempFilteredRecords.RemoveAt(rndIndex);
                Dispatcher.Invoke(() => app.progressBar.pbStatus.Value = index);
            }
            return listSplitRecords;
        }

        private int GetIndexOfImagePairWithorWithoutClass(List<string> listTempFilteredRecords, string strImageName, string strClass = "")
        {
            int index = -1;

            if(string.IsNullOrEmpty(strClass))
            {
                if (strImageName.Contains(app.settings.SinglePhase))
                    index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.PhaseContrast))));

                else if (strImageName.Contains(app.settings.PhaseContrast))
                    index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.SinglePhase))));
            }
            else
            {
                if (strImageName.Contains(app.settings.SinglePhase))
                    index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.PhaseContrast)) && item.Contains(strClass)));

                else if (strImageName.Contains(app.settings.PhaseContrast))
                    index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.SinglePhase)) && item.Contains(strClass)));

                if (index == -1)
                {
                    if (strImageName.Contains(app.settings.SinglePhase))
                        index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.PhaseContrast))));

                    else if (strImageName.Contains(app.settings.PhaseContrast))
                        index = listTempFilteredRecords.IndexOf(listTempFilteredRecords.FirstOrDefault(item => (item.Contains(strImageName.Split('.').FirstOrDefault() + app.settings.SinglePhase))));
                }
            }

            return index;
        }

        private void txtSplit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void txtSplit_TextChanged(object sender, TextChangedEventArgs e)
        {
            int nSplit1 = txtSplit1.Text == string.Empty ? 0 : Convert.ToInt16(txtSplit1.Text);
            int nSplit2 = txtSplit2.Text == string.Empty ? 0 : Convert.ToInt16(txtSplit2.Text);

            if ((sender as TextBox).Name == "txtSplit1" && radTwo.IsChecked.Value)
                txtSplit2.Text = (100 - nSplit1).ToString();
            else if (radThree.IsChecked.Value)
                txtSplit3.Text = (100 - (nSplit1 + nSplit2)) < 0 ? "0" : (100 - (nSplit1 + nSplit2)).ToString();

            if (nSplit1 == 0)
            {
                txtSplit2.Text = string.Empty;
                txtSplit3.Text = string.Empty;
            }
            else if(nSplit2 == 0)
                txtSplit3.Text = string.Empty;
        }

        private void listLoadedFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (app.settings.ImportFilePath == null || app.settings.ImportFilePath.Length == 1)
                return;

            arrSelDatasheet = listLoadedFile.SelectedItems.Cast<string>().ToArray();
            ListClassFormat = app.GetClassFormatList(arrSelDatasheet);
            ListClassView.ItemsSource = ListClassFormat;
        }

        private void tabSplit_SelectionChanged(object sender, Telerik.Windows.Controls.RadSelectionChangedEventArgs e)
        {
            tabSplit.SelectedIndex = currentTabIndex;
        }
    }
}
