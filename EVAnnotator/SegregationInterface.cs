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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public void CSVExportSegregatedImages(object sender, DoWorkEventArgs e)
        {
            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Nothing to export..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (TotalMultiClassLabelled == 0)
            {
                System.Windows.MessageBox.Show("Segregated Images are not found..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            try
            {
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                string strDTNow = DateTime.Now.ToString("ddMMyyyy_HHmmss");

                StringBuilder sbContents = new StringBuilder();
                sbContents.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname, "Type : " + settings.ClassType));

                string strDataPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\" + settings.CSVExportFolder;
                string strSourceDataSetPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\" + settings.sourceFolder;

                //string strDataPath = settings.CSVExportPath + @"\Output Data\Segregated CSV";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);
                if (Directory.Exists(strSourceDataSetPath))
                    Directory.Delete(strSourceDataSetPath, true);
                Directory.CreateDirectory(strSourceDataSetPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "classification_" + strDTNow + ".csv");
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.InitialDirectory = strDataPath;
                saveFileDialog.Filter = "csv file|*.csv";
                saveFileDialog.FileName = "classification_" + strDTNow + ".csv";

                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
                Dispatcher.Invoke(() =>
                {
                    result = saveFileDialog.ShowDialog();
                });

                if (result == System.Windows.Forms.DialogResult.OK)
                    strCSVSavePath = saveFileDialog.FileName;
                else
                    return;

                Dispatcher.Invoke(() => busyIndicator.IsBusy = true);
                bool bIsSourceCopy = true;
                //MessageBoxResult res = System.Windows.MessageBox.Show(string.Format("Please Confirm to copy segregated images to source folder?"), "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (res == MessageBoxResult.Yes)
                //{
                //    bIsSourceCopy = true;
                //}

                sbContents.AppendLine(string.Join(",", "filename", "region_count", "segregated_class", "to_be_corrected"));
                for (int nCount = 0; nCount < ImageMenuList.Count; nCount++)
                {
                    ImageMenu curImage = ImageMenuList[nCount];
                    ImageListBox curImageBox = curImage.ImageBox;
                    bool bIsCopied = true;
                    if (bIsSourceCopy)
                    {
                        string sourceFilePath = curImage.ImagePath;
                        string strClass = curImageBox.ListImageClass.Count > 0? curImageBox.ListImageClass.First().ClassName : "UnLabelled";
                        strClass = strClass.Split('(', ')').Length > 0 ? strClass.Split('(', ')')[0] : "";
                        string destFilePath = strSourceDataSetPath + "\\" + strClass;
                        if (!Directory.Exists(destFilePath))
                            Directory.CreateDirectory(destFilePath);

                        destFilePath = destFilePath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                        try
                        {
                            if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            bIsCopied = true;
                        }
                        catch { bIsCopied = false; }
                    }

                    if (!bIsCopied)
                        continue;
                    if (curImageBox.ListImageClass.Count > 0) 
                    {
                        try
                        {                            
                            for (int nClasscnt = 0; nClasscnt < curImageBox.ListImageClass.Count; nClasscnt++) {
                                ImageClass curImageclass = curImageBox.ListImageClass[nClasscnt];
                                string strRegion = "\"{\"\"class id\"\":\"\"" + curImageclass.ClassIndex + "\"\", \"\"class name\"\":\"\"" + curImageclass.ClassAlias + "\"\"}\"";
                                string strReviewed = curImageclass.Reviewed ? "Yes" : "No";
                                sbContents.Append(curImageBox.ImageBoxName);
                                sbContents.Append(",");
                                sbContents.Append(curImageBox.ListImageClass.Count.ToString());
                                sbContents.Append(",");
                                sbContents.Append(strRegion);
                                sbContents.Append(",");
                                sbContents.Append(strReviewed);
                                sbContents.AppendLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.LogMessage("CSVExportRectangleImages: " + ex.Message, 9);
                        }
                    }
                    else
                    {
                        string strAttributes = curImage.ImageName + ",0,{},";
                        sbContents.AppendLine(strAttributes);
                    }
                }

                File.WriteAllText(strCSVSavePath, sbContents.ToString());
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                System.Windows.MessageBox.Show("Segregated Image CSV exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Segregated Image CSV exported.", 0);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                MessageBox.Show("Export Failed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("CSVExportSegregatedImages: " + ex.Message, 9);
            }
        }

        public void CSVExportRectangleImages(object sender, DoWorkEventArgs e)
        {
            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Nothing to export..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (TotalMultiClassLabelled == 0)
            {
                System.Windows.MessageBox.Show("Labelled Images are not found..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                            MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            try
            {
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                string strDTNow = DateTime.Now.ToString("ddMMyyyy_HHmmss");

                StringBuilder sbContents = new StringBuilder();
                sbContents.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname, "Type : " + settings.ClassType));

                string strDataPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\" + settings.CSVExportFolder;
                string strSourceDataSetPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\" + settings.sourceFolder;
                //string strDataPath = settings.CSVExportPath + @"\Output Data\Segregated CSV";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);
                if (Directory.Exists(strSourceDataSetPath))
                    Directory.Delete(strSourceDataSetPath, true);
                Directory.CreateDirectory(strSourceDataSetPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "rectangle_" + strDTNow + ".csv");
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.InitialDirectory = strDataPath;
                saveFileDialog.Filter = "csv file|*.csv";
                saveFileDialog.FileName = "rectangle_" + strDTNow + ".csv";

                System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
                Dispatcher.Invoke(() =>
                {
                    result = saveFileDialog.ShowDialog();
                });

                if (result == System.Windows.Forms.DialogResult.OK)
                    strCSVSavePath = saveFileDialog.FileName;
                else
                    return;

                Dispatcher.Invoke(() => busyIndicator.IsBusy = true);
                bool bIsSourceCopy = true;
                //MessageBoxResult res = System.Windows.MessageBox.Show(string.Format("Please Confirm to copy labelled images to source folder?"), "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (res == MessageBoxResult.Yes)
                //{
                //    bIsSourceCopy = true;
                //}

                sbContents.AppendLine("filename,region_count,region_shape_attributes,region_attributes,to_be_corrected,line_name");
                for (int nCount = 0; nCount < ImageMenuList.Count; nCount++)
                {
                    ImageMenu curImage = ImageMenuList[nCount];
                    ImageListBox curImageBox = curImage.ImageBox;
                    bool bIsCopied = true;
                    if (bIsSourceCopy)
                    {
                        string sourceFilePath = curImage.ImagePath;
                        string destFilePath = strSourceDataSetPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                        try
                        {
                            if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            bIsCopied = true;
                        }
                        catch { bIsCopied = false; }
                    }
                    if (!bIsCopied)
                        continue;

                    if (curImageBox.ListImageClass.Count > 0) 
                    {
                        try
                        {                            
                            for (int nClasscnt = 0; nClasscnt < curImageBox.ListImageClass.Count; nClasscnt++) {
                                ImageClass curImageclass = curImageBox.ListImageClass[nClasscnt];
                                string strRegionCount = curImageBox.ListImageClass.Count.ToString();
                                string classID = curImageclass.ClassIndex;
                                string strRegion = "{\"class id\":\"" + classID + "\", \"class name\":\"" + curImageclass.ClassAlias + "\"}";
                                string strReviewed = curImageclass.Reviewed ? "Yes" : "No";
                                string strLineName = "";
                                string strAttributes = curImageBox.ImageBoxName + "," + strRegionCount + ",\"" + curImageclass.ShapeCoordinates.Replace("\"", "\"\"") + "\",\""
                                                    + strRegion.Replace("\"", "\"\"") + "\"," + strReviewed + "," + strLineName;
                                sbContents.AppendLine(strAttributes);
                            }
                        }
                        catch (Exception ex) 
                        { 
                            Utilities.LogMessage("CSVExportRectangleImages: " + ex.Message, 9); 
                        }                        
                    }
                    else
                    {
                        string strAttributes = curImage.ImageName + ",0,{},{},,";
                        sbContents.AppendLine(strAttributes);
                    }
                }

                File.WriteAllText(strCSVSavePath, sbContents.ToString());
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                System.Windows.MessageBox.Show("Labelled Image CSV exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Labelled Image CSV exported.", 0);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                Dispatcher.Invoke(() => busyIndicator.IsBusy = false);
                MessageBox.Show("Export Failed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("CSVExportRectangleImages: " + ex.Message, 9);
            }
        }

        public bool LoadSegregatedImagesFromCSV()
        {
            try
            {
                //settings.nImportFileRecordCount = new int[settings.ImportFilePath.Length];
                //labelEvent.Reset();
                //SaveEvent.Reset();
                //CleanupLoadedData();
                DictColHeaders = new Dictionary<string, string[]>();

                for (int i = 0; i < settings.ImportFilePath.Length; i++)
                {
                    List<string[]> listCSVData = new List<string[]>();
                    ListDatasheetImportData.Add(new ImportDatasheetData(settings.ImportFilePath[i])
                    {
                        ListImportData = listCSVData
                    });
                    List<string> listCSVlines = File.ReadAllLines(settings.ImportFilePath[i]).ToList();
                    DictColHeaders[settings.ImportFilePath[i]] = GetColumnHeaderNames(listCSVlines);

                    string[] split = listCSVlines[0].Split(',');
                    if (!IsValidProjectDataSheet(split))
                    {
                        //OnWorkerMethodComplete("complete");
                        System.Windows.MessageBox.Show("Project with architecture does not match with Project in CSV file selected..!\nPlease Select proper CSV file or change Project in File->Settings Configuration window..", "File Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                                MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                        return false;
                    }

                    listCSVlines = RemoveSegregationCSVListValues(listCSVlines);
                    Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = listCSVlines.Count);
                    for (int lineCount = 0; lineCount < listCSVlines.Count; lineCount++)
                    {
                        Dispatcher.Invoke(() => progressBar.pbStatus.Value = lineCount);
                        string[] lineSplit = Regex.Split(listCSVlines[lineCount], @"(?<!,[^[]+\{[^}]+),");
                        listCSVData.Add(lineSplit);
                        lineSplit = lineSplit.Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                        if (!IsValidCSVLine(lineSplit))
                            continue;

                        string ImageName = lineSplit[0];
                        char strImageType = ImageName.Contains(settings.SinglePhase) ? 'S' : ImageName.Contains(settings.PhaseContrast) ? 'P' : ' ';
                        ImageListBox curImageBox = ProcessedImageBox.Find(item => item.ImageBoxName == ImageName);
                        string ClassName;
                        string ClassID = Regex.Match(lineSplit[2], @"\b[:]\s*[0-9]+").ToString().Replace(":", "").Trim();
                        string ClassFolderName = Regex.Match(lineSplit[2], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim();
                        if (ClassID == string.Empty || ClassFolderName == string.Empty)
                            continue;

                        ClassName = settings.dictEVSupervisorClass.Keys.ToList().Contains(int.Parse(ClassID)) ? settings.dictEVSupervisorClass[int.Parse(ClassID)].ToString() : ClassFolderName + "(" + ClassFolderName + ")";

                        ClassFolderStat curclassFolder = ListClassFolderStat.FirstOrDefault(item => item.ClassAliasName.ToUpper() == ClassFolderName.ToUpper() && item.ImportDatasheetName == settings.ImportFilePath[i].Trim());
                        if (curclassFolder == null)
                        {
                            ListClassFolderStat.Add(new ClassFolderStat
                            {
                                ImportDatasheetName = settings.ImportFilePath[i].Trim(),
                                ClassCount = 1,
                                ClassAliasName = ClassFolderName,
                                ClassID = ClassID,
                                SingleSpotCount = strImageType == 'S' ? 1 : 0,
                                PhaseContrastCount = strImageType == 'P' ? 1 : 0
                            });
                        }
                        else
                        {
                            curclassFolder.ClassCount++;
                            if (strImageType == 'S')
                                curclassFolder.SingleSpotCount++;
                            else if (strImageType == 'P')
                                curclassFolder.PhaseContrastCount++;
                        }

                        ImageClass curImageclass = new ImageClass(ClassID, ClassName);
                        curImageclass.ClassAlias = ClassFolderName;
                        curImageclass.ImportDatasheetName = settings.ImportFilePath[i];
                        
                        bool reviewed = false;
                        if (lineSplit.Length > 3 && lineSplit[3] != "")
                            reviewed = lineSplit[3] == "Yes" ? true : false;

                        curImageclass.Reviewed = reviewed;
                        if (curImageBox == null)
                        {
                            curImageBox = new ImageListBox(ImageName);
                            ProcessedImageBox.Add(curImageBox);
                        }
                        Dispatcher.Invoke(() => { curImageBox.ListImageClass.Add(curImageclass); });
                    }
                    settings.nImportFileRecordCount[i] = listCSVlines.Count;
                }
                TotalDataSheet = settings.ImportFilePath.Length;
                TotalRecordFound = settings.nImportFileRecordCount.Sum();
                TotalViolationFound = 0;

                //ImageClassMatching();
                //if (ImageMenuList.Count > 0)
                //    Dispatcher.Invoke(() => {
                //        ListBoxImages_SelectionChanged(null, null);
                //    });
                
                //labelEvent.Set();
                //SaveEvent.Set();
                //bIsLoadLabellingGraph = true;
                //OnWorkerMethodComplete("complete");
                //System.Windows.MessageBox.Show("Segregated Image CSV File Imported Successfully.", "File Import", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return true;
            }

            catch (System.Exception ex)
            {
                //labelEvent.Set();
                //SaveEvent.Set();
                //OnWorkerMethodComplete("Complete");
                //MessageBox.Show("Segregation CSV Import failed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("LoadSegregatedImagesFromCSV: " + ex.Message, 9);
                return false;
            }
        }

        public void ExportSegregatedImagesToDisk()
        {
            try
            {
                labelEvent.Reset();
                ImageMenu[] arrImageMenuList = ImageMenuList.Where(item => item.MenuItemBrush != ImageMenuBrushes[0]).ToArray();

                string strDataPath = settings.CSVExportPath + @"\Output Data\Segregated Images";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                System.Windows.Forms.FolderBrowserDialog openDialog = new System.Windows.Forms.FolderBrowserDialog();
                openDialog.SelectedPath = strDataPath;
                openDialog.ShowNewFolderButton = true;

                bool bIsReturn = false;
                Dispatcher.Invoke(() =>
                {
                    System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                        strDataPath = openDialog.SelectedPath;
                    else
                        bIsReturn = true;
                });

                if (bIsReturn)
                {
                    OnWorkerMethodComplete("Complete");
                    labelEvent.Set();
                    return;
                }

                List<string> listImagePath = arrImageMenuList.Select(item => item.ImagePath).ToList();
                if (!IsSegregationDiskSpaceOK(strDataPath, listImagePath))
                {
                    OnWorkerMethodComplete("Complete");
                    System.Windows.MessageBox.Show("Output disk was full, Cannot Copy/Move images.!. Free Some space and try again..", "No Storage Space", MessageBoxButton.OK,
                            MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    if (Directory.Exists(strDataPath))
                        Directory.Delete(strDataPath);
                    return;
                }

                Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = arrImageMenuList.Length);
                for (int nCount = 0; nCount < arrImageMenuList.Length; nCount++)
                {
                    Dispatcher.Invoke(() => progressBar.pbStatus.Value = nCount);

                    ImageMenu curImage = ImageMenuList[nCount];
                    ImageListBox curImageBox = curImage.ImageBox;

                    List<string> listClassNames = curImageBox.ListImageClass.Select(item => item.ClassName).Distinct().ToList();
                    string strFolderName = string.Empty;
                    foreach (string curClass in listClassNames)
                    {
                        string strClass = curClass.Split('(').Length > 0 ? curClass.Split('(')[0] : curClass;
                        if (strFolderName != string.Empty)
                            strFolderName += "_" + strClass;
                        else
                            strFolderName = strClass;
                    }

                    strFolderName = Path.Combine(strDataPath, strFolderName);
                    if (!Directory.Exists(strFolderName))
                        Directory.CreateDirectory(strFolderName);

                    string strDesImagePath = Path.Combine(strFolderName, curImage.ImageName);
                    Alphaleonis.Win32.Filesystem.File.Copy(curImage.ImagePath, strDesImagePath, true);
                }

                OnWorkerMethodComplete("Complete");
                labelEvent.Set();
                MessageBox.Show("Segregated images exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                labelEvent.Set();
                OnWorkerMethodComplete("Complete");
                MessageBox.Show("Exporting Segregated images failed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("SegregationInterface: ExportSegregatedImagesToDisk" + ex.Message, 9);
            }
        }

        public bool IsSegregationDiskSpaceOK(string strDataPath, List<string> listImagePath)
        {
            ulong SourceImageSize = 0;

            for (int count = 0; count < listImagePath.Count; count++)
            {
                string strSourcePath = listImagePath[count];
                if (!string.IsNullOrEmpty(strSourcePath) && File.Exists(strSourcePath))
                {
                    try
                    {
                        Alphaleonis.Win32.Filesystem.FileInfo file = new Alphaleonis.Win32.Filesystem.FileInfo(strSourcePath);
                        SourceImageSize += Convert.ToUInt64(file.Length);
                    }
                    catch { }
                }
            }

            bool isDiskSpaceOk = Utilities.CheckDiskSpaceOK(strDataPath, SourceImageSize);
            return isDiskSpaceOk;
        }

        public void SaveLabelledWorkintoDisk()
        {            
            if (ImageMenuList.Count == 0 || ProcessedImageBox.Count == 0)
            {
                return;
            }
            bool triggered = SaveEvent.WaitOne(10);
            if (!triggered)
            {
                return;
            }

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_SaveLabelledWorkStat;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStart("Save");
        }

        public void SaveLabelledWorkStats()
        {
            try
            {
                SaveEvent.Reset();
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                {
                    OnWorkerMethodComplete("Complete");
                    SaveEvent.Set();
                    return;
                }

                string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
                string ProjectSavedWork = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";
                if (!Directory.Exists(Workdir))
                    Directory.CreateDirectory(Workdir);
                if (!Directory.Exists(ProjectSavedWork))
                    Directory.CreateDirectory(ProjectSavedWork);

                string[] StatsFile = Directory.GetFiles(Workdir, "*Savedata*.bin");
                string[] ProjectStatsFile = Directory.GetFiles(ProjectSavedWork, "*Savedata*.bin");
                string serializationFile = Path.Combine(Workdir, "Savedata_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");
                string Project_serializationFile = Path.Combine(ProjectSavedWork, "Savedata_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");

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
                            bformatter.Serialize(stream, curImageClass.ImportDatasheetName != null ? curImageClass.ImportDatasheetName : "");
                        }
                    }

                    //Delete the old file
                    if (StatsFile.Length > 0)
                    {
                        foreach (string file in StatsFile)
                            File.Delete(file);
                    }
                    if (ProjectStatsFile.Length > 0)
                    {
                        foreach (string file in ProjectStatsFile)
                            File.Delete(file);
                    }
                    //Save to new file
                    Stream FileStream = File.Open(serializationFile, FileMode.Create);
                    stream.WriteTo(FileStream);
                    FileStream.Close();

                    FileStream = File.Open(Project_serializationFile, FileMode.Create);
                    stream.WriteTo(FileStream);
                    FileStream.Close();
                }

                OnWorkerMethodComplete("Complete");
                SaveEvent.Set();
                Dispatcher.Invoke(() => Utilities.LogMessage("Labelled works saved into disk successfully."));
            }

            catch (Exception ex)
            {
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
                Dispatcher.Invoke(() => Utilities.LogMessage("Saving work while closing app failed: @ line number: " + lineNumber + " " + ex.Message, 9));
                SaveEvent.Set();
            }
        }

        public void LoadLabelledWorkStats()
        {
            try
            {
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
                string[] StatsFile = Directory.GetFiles(Workdir, "*Savedata*.bin");
                if (StatsFile.Length == 0)
                    return;

                string deSerializFile = System.IO.Path.Combine(Workdir, StatsFile[0]);
                var converter = new System.Windows.Media.BrushConverter();

                using (Stream stream = File.Open(deSerializFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    string projectKey = (string)bformatter.Deserialize(stream);
                    string Architecture = (string)bformatter.Deserialize(stream);

                    if (settings.CurrentProject != projectKey && settings.Architecture != Architecture)
                    {
                        OnWorkerMethodComplete("Complete");
                        return;
                    }
                    settings.LoadImagePath = (string)bformatter.Deserialize(stream);
                    TotalImagesPresent = (int)bformatter.Deserialize(stream);
                    TotalImagesLoaded = (int)bformatter.Deserialize(stream);
                    TotalDuplicateImages = (int)bformatter.Deserialize(stream);
                    settings.LoadedImageSize = 0;
                    int imageCount = (int)bformatter.Deserialize(stream);

                    for (int count = 0; count < imageCount; count++)
                    {
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
                    for (int i = 0; i < processCount; i++)
                    {
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

                if (ImageMenuList != null && ImageMenuList.Count > 0 && ProcessedImageBox != null && ProcessedImageBox.Count > 0)
                {
                    foreach (ImageListBox curImageBox in ProcessedImageBox) {
                        ImageMenu curImageMenu = ImageMenuList.FirstOrDefault(item => item.ImageName == curImageBox.ImageBoxName);
                        if (curImageMenu != null)
                            curImageMenu.ImageBox = curImageBox;
                    }
                    //LoadAllVisualizationGraphs();
                }
                
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
                });

                if (threadCheckLabelling == null)
                {
                    threadCheckLabelling = new Thread(CheckLabellingThread);
                    threadCheckLabelling.IsBackground = true;
                    threadCheckLabelling.Start();
                    threadCheckLabelling.Priority = ThreadPriority.Lowest;
                }
                OnWorkerMethodComplete("Complete");
                ShowStatusBarLabel("Last saved work has been loaded successfully", 3, false);

                Utilities.LogMessage("Last saved work has been loaded successfully");
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
                Utilities.LogMessage("Error while loading last saved work.");
                Utilities.LogMessage("LoadSavedWorkHistory " + ex.Message, 9);
            }
        }

        public void SegregateAllLoadedImages(object[] args)
        {
            try
            {

                bool bIsIncludLabelled = false;
                string strClassName = string.Empty;

                Dispatcher.Invoke(() =>
                {
                    bIsIncludLabelled = (bool)args[0];
                    strClassName = (string)args[1];
                });

                labelEvent.Reset();
                ImageMenu[] arrImageMenuList = bIsIncludLabelled? ImageMenuList.ToArray() : ImageMenuList.Where(item => item.MenuItemBrush == ImageMenuBrushes[0]).ToArray();

                if (!settings.dictEVSupervisorClass.ContainsValue(strClassName))
                {
                    MessageBox.Show("Invalid ClassName selected. Please select proper classname to continue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                int classID = settings.dictEVSupervisorClass.FirstOrDefault(s => s.Value == strClassName).Key;
                Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = arrImageMenuList.Length);
                int i = 0;
                foreach (ImageMenu curImageMenu in arrImageMenuList)
                {
                    Dispatcher.Invoke(() => progressBar.pbStatus.Value = ++i);
                    ImageListBox imageListBox = curImageMenu.ImageBox;
                    ImageClass curClassAttribute = new ImageClass(classID.ToString(), strClassName);
                    curClassAttribute.ClassAlias = strClassName.Split('(', ')').Length > 1 ? strClassName.Split('(', ')')[1]
                                                    : strClassName.Split('(', ')')[0];

                    Dispatcher.Invoke(() =>
                    {
                        imageListBox.ListImageClass.Clear();
                        imageListBox.ListImageClass.Insert(0, curClassAttribute);
                    });
                }

                OnWorkerMethodComplete("Complete");
                labelEvent.Set();
                MessageBox.Show(arrImageMenuList.Length + " loaded Images Segregated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                labelEvent.Set();
                OnWorkerMethodComplete("Complete");
                MessageBox.Show("Segregation of loaded images failed..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("SegregationInterface: SegregateAllLoadedImages" + ex.Message, 9);
            }
        }
    }
}
