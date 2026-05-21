using GenieSupervisor.Properties;
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telerik.Windows.Controls.DataVisualization.Map.BingRest;
using Telerik.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for DataFolderSplitter.xaml
    /// </summary>
    public partial class DataFolderSplitter : Window
    {
        MainWindow app;
        string Operation;
        BackgroundWorker BGWorkerFormat;
        int[] splitPercent = new int[3];
        int[] arrTrainValues = new int[] { 80, 70, 60, 50 };
        //int[] arrValidationValues = new int[] { 50, 40, 30, 20, 15, 10 };
        //int[] arrTestValues = new int[] { 40, 30, 20, 15, 10, 0 };
        bool bIsPatchCore = false;

        public DataFolderSplitter(MainWindow app, string strOperation)
        {
            InitializeComponent();
            this.app = app;
            Operation = strOperation;

            spRegion.Visibility = app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline ? Visibility.Visible : Visibility.Collapsed;
            lblLabelImage.Content = app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline ? "Labelled Images : " : "Segregated Images : ";
            lblImageCount.Content = app.TotalLabelledImages;
            int totalClassCount = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0 && item.MenuItemBrush != app.ImageMenuBrushes[2]).SelectMany(s => s.ImageBox.ListImageClass).Count();
            lblRegionCount.Content = totalClassCount;
            bIsPatchCore = app.settings.Architecture == app.settings.PatchcoreAlias;
            lblHeading.Content = bIsPatchCore ? "Train/Test dataset splitter" : "Train/Val/Test dataset splitter";
            spVal.Visibility = bIsPatchCore ? Visibility.Collapsed : Visibility.Visible;
            cmbTrain.ItemsSource = arrTrainValues;
            cmbTrain.SelectedIndex = 0;

            if (bIsPatchCore)
            {
                gridMain.Visibility = Visibility.Collapsed;
                gridPatchcore.Visibility = Visibility.Visible;
                LoadPatchcoreClassSummary();
                UpdatePatchcoreSplitCounts();
            }

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

        private void btnClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        //private void txtSplit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    Regex regex = new Regex("[^0-9]+");
        //    if (regex.IsMatch(e.Text) || txtFolder1.Text.Length > 1)
        //    {
        //        e.Handled = true;
        //    }
        //}

        private void btnProceed_Click(object sender, RoutedEventArgs e)
        {
            if (splitPercent[0] == 0 || splitPercent[1] == 0)
            {
                System.Windows.MessageBox.Show("Train/Validation set ratio cannot be blank or zero", "Blank", MessageBoxButton.OK);
                return;
            }

            var tempImageList = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0 && item.MenuItemBrush != app.ImageMenuBrushes[2]).ToList();
            List<string> listNotEnoughClass = new List<string>();
            foreach (string strClass in app.settings.ListEVSupervisorClassAlias)
            {
                int count = tempImageList.Sum(item => item.ImageBox.ListImageClass.Where(s => s.ClassAlias.ToUpper() == strClass.ToUpper()).Count());
                if (count < 5)
                    listNotEnoughClass.Add(strClass);
            }
            if (tempImageList.Count < 10)
            {
                System.Windows.MessageBox.Show("Dataset should contain minimum 10 " + lblLabelImage.Content.ToString().Trim().Replace(":", "") + "to split train/val/test set.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None);
                return;
            }
            else if (listNotEnoughClass.Count > 0)
            {
                if (listNotEnoughClass.Count == app.settings.ListEVSupervisorClassAlias.Count)
                {
                    if (app.settings.ClassType != EnumClassType.Segregation)
                        System.Windows.MessageBox.Show("Atlease one class must have more than 5 annotation in labelled images. Please label more images from loaded dataset to continue.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning,
                                            MessageBoxResult.None);
                    else
                        System.Windows.MessageBox.Show("Atlease one class must have more than 5 segreagated images. Please segregate more images from loaded dataset to continue.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning,
                                            MessageBoxResult.None);
                    return;
                }

                string strClasses = listNotEnoughClass.Count > 1 ? string.Join(", ", listNotEnoughClass) : listNotEnoughClass[0];
                MessageBoxResult result;
                this.IsEnabled = false;
                if (app.settings.ClassType != EnumClassType.Segregation)
                    result = System.Windows.MessageBox.Show("The following classes do not have enough labelled annotations. Each class must have a minimum of 5 annotations : \n" +
                            strClasses + "\nDo you still want to continue without including above class records in the train/val/test data split folders?", "Confirm", MessageBoxButton.YesNo,
                            MessageBoxImage.Question, MessageBoxResult.None);
                else
                    result = System.Windows.MessageBox.Show("The following classes do not have enough segregated images. Each class must have a minimum of 5 segregated images : \n" +
                            strClasses + "\nDo you still want to continue without including above class records in the train/val/test data split folders?", "Confirm", MessageBoxButton.YesNo,
                            MessageBoxImage.Question, MessageBoxResult.None);

                if (app.settings.ClassType != EnumClassType.Segregation)
                    Utilities.LogMessage("The following classes do not have enough labelled annotations. Each class must have a minimum of 5 annotations", 0);
                else
                    Utilities.LogMessage("The following classes do not have enough segregated images. Each class must have a minimum of 5 segregated images", 0);

                this.IsEnabled = true;
                if (result == MessageBoxResult.No)
                    return;
            }

            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            app.busyIndicator.IsBusy = true;
            BGWorkerFormat.DoWork += bgwDowork_DataSheetSplit;
            BGWorkerFormat.RunWorkerAsync(listNotEnoughClass);
            this.Close();
        }

        private void bgwDowork_DataSheetSplit(object sender, DoWorkEventArgs e)
        {
            List<string> listClass = e.Argument as List<string>;
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadSplit = new Thread(() => SplitDataFolderintoParts(listClass));
                threadSplit.IsBackground = true;
                threadSplit.Start();
            }
        }

        private void SplitDataFolderintoParts(List<string> listExcludeClass)
        {
            try
            {
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
                string strValDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.valFolder;
                string strTrainDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.trainFolder;
                string strTestSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.testsetFolder;
                string strSourceDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.sourceFolder;

                if (app.settings.LoadImagePath.Contains(strTrainDataSetPath) || app.settings.LoadImagePath.Contains(strValDataSetPath) ||
                    app.settings.LoadImagePath.Contains(strTestSetPath) || app.settings.LoadImagePath.Contains(strSourceDataSetPath))
                {
                    Dispatcher.Invoke(() => {
                        app.busyIndicator.IsBusy = false;
                        System.Windows.MessageBox.Show("Error while Datasplit operation! \nImages are loaded from train/val/test/source path. Cannot continue operation." +
                        "\nLoad images from other path and continue dataspilt process.", "Abort", MessageBoxButton.OK,
                                    MessageBoxImage.Error, MessageBoxResult.None);
                        Utilities.LogMessage("Error while Datasplit operation. Images are loaded from train/val/test/source path. Cannot continue operation", 0);
                    });
                    return;
                }

                if (app.settings.ClassType != EnumClassType.Segregation)
                {
                    string strcsvFile = System.IO.Path.Combine(strTrainDataSetPath, "train.csv");
                    if (File.Exists(strcsvFile))
                        System.IO.File.SetAttributes(strcsvFile, System.IO.FileAttributes.Normal);

                    strcsvFile = System.IO.Path.Combine(strValDataSetPath, "val.csv");
                    if (File.Exists(strcsvFile))
                        System.IO.File.SetAttributes(strcsvFile, System.IO.FileAttributes.Normal);

                    strcsvFile = System.IO.Path.Combine(strTestSetPath, "test.csv");
                    if (File.Exists(strcsvFile))
                        System.IO.File.SetAttributes(strcsvFile, System.IO.FileAttributes.Normal);
                }

                if (Directory.Exists(strValDataSetPath))
                    Directory.Delete(strValDataSetPath, true);
                Directory.CreateDirectory(strValDataSetPath);

                if (Directory.Exists(strTrainDataSetPath))
                    Directory.Delete(strTrainDataSetPath, true);
                Directory.CreateDirectory(strTrainDataSetPath);

                if (Directory.Exists(strTestSetPath))
                    Directory.Delete(strTestSetPath, true);
                Directory.CreateDirectory(strTestSetPath);

                if (Directory.Exists(strSourceDataSetPath))
                    Directory.Delete(strSourceDataSetPath, true);
                Directory.CreateDirectory(strSourceDataSetPath);

                List<ImageMenu> listLabelledImages = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0 && item.MenuItemBrush != app.ImageMenuBrushes[2]).ToList();
                //Utilities.Shuffle(listLabelledImages);

                List<string> listImagePath = listLabelledImages.Select(item => item.ImagePath).ToList();
                if (!app.IsSegregationDiskSpaceOK(app.ConfigFilePath, listImagePath))
                {
                    //app.OnWorkerMethodComplete("Complete");
                    Dispatcher.Invoke(() => {
                        app.busyIndicator.IsBusy = false;
                        System.Windows.MessageBox.Show("Output disk was full, Cannot Copy images.!. Free Some space and try again..", "No Storage Space", MessageBoxButton.OK,
                                MessageBoxImage.Error, MessageBoxResult.None);
                    });
                    Utilities.LogMessage("Output disk was full, Cannot Copy images. Free Some space and try again.", 0);
                    return;
                }

                bool bIsSourceCopy = true;
                Dispatcher.Invoke(() => bIsSourceCopy = chkCopySource.IsChecked.Value);

                GenerateClassModelini();

                int[] nTotalSplitDataCount = new int[3];
                string strClassNametxtPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\classnames.txt";
                List<string> listClass = app.settings.ListEVSupervisorClassAlias.Except(listExcludeClass).ToList();   // app.settings.dictEVSupervisorClass.Values.ToList();
                List<string> listClassNames = new List<string>();
                if (app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline)
                {
                    string strImageName = app.settings.Architecture == app.settings.DetectionAlias || app.settings.Architecture == app.settings.SegmentationAlias ? "images" : "Images";
                    string strTrainImagesPath = System.IO.Path.Combine(strTrainDataSetPath, strImageName);
                    if (!Directory.Exists(strTrainImagesPath))
                        Directory.CreateDirectory(strTrainImagesPath);
                    string strTrainCSVPath = System.IO.Path.Combine(strTrainDataSetPath, "train.csv");
                    StringBuilder sbContent = new StringBuilder();
                    sbContent.AppendLine("filename,region_count,region_shape_attributes,region_attributes");
                    File.WriteAllText(strTrainCSVPath, sbContent.ToString());

                    string strValImagesPath = System.IO.Path.Combine(strValDataSetPath, strImageName);
                    if (!Directory.Exists(strValImagesPath))
                        Directory.CreateDirectory(strValImagesPath);
                    string strValCSVPath = System.IO.Path.Combine(strValDataSetPath, "val.csv");
                    File.WriteAllText(strValCSVPath, sbContent.ToString());

                    string strTestCSVPath = System.IO.Path.Combine(strTestSetPath, "test.csv");
                    File.WriteAllText(strTestCSVPath, sbContent.ToString());

                    for (int index = 0; index < listClass.Count; index++)
                    {
                        string curClass = listClass[index];
                        listClassNames.Add(curClass);

                        var listFilterdImages = listLabelledImages.SelectMany(item => item.ImageBox.ListImageClass.Where(menu => menu.ClassAlias.ToUpper() == curClass.ToUpper()).
                                                    Select(menu => new
                                                    {
                                                        ImagePath = item.ImagePath,
                                                        ImageName = item.ImageBox.ImageBoxName,
                                                        RegionCount = item.ImageBox.ListImageClass.Count,
                                                        ClassAlias = menu.ClassAlias,
                                                        Coordinates = menu.ShapeCoordinates
                                                    })).ToList();
                        if (listFilterdImages.Count == 0)
                            continue;

                        int[] nSplitCount = new int[3];
                        nSplitCount[0] = Convert.ToInt32(Math.Round((listFilterdImages.Count * splitPercent[0]) * 1.0 / 100, 0));
                        nSplitCount[1] = Convert.ToInt32(Math.Ceiling((listFilterdImages.Count * splitPercent[1]) * 1.0 / 100));
                        nSplitCount[2] = listFilterdImages.Count - (nSplitCount[0] + nSplitCount[1]);

                        if (nSplitCount[2] > nSplitCount[1])
                        {
                            int temp = nSplitCount[2];
                            nSplitCount[2] = nSplitCount[1];
                            nSplitCount[1] = temp;
                        }

                        if (nSplitCount[2] < 0)
                            nSplitCount[2] = 0;

                        nTotalSplitDataCount[0] += nSplitCount[0];
                        nTotalSplitDataCount[1] += nSplitCount[1];
                        nTotalSplitDataCount[2] += nSplitCount[2];

                        //For Train folder
                        sbContent = new StringBuilder();
                        for (int count = 0; count < nSplitCount[0]; count++)
                        {
                            string sourceFilePath = listFilterdImages[count].ImagePath;
                            string destFilePath = strTrainImagesPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);

                                string strRegion = "{\"class id\":\"" + index + "\", \"class name\":\"" + curClass + "\"}";
                                string strAttributes = listFilterdImages[count].ImageName + "," + listFilterdImages[count].RegionCount.ToString() + ",\"" + listFilterdImages[count].Coordinates.Replace("\"", "\"\"") + "\",\"" + strRegion.Replace("\"", "\"\"") + "\"";
                                sbContent.AppendLine(strAttributes);
                            }
                            catch { }
                        }
                        File.AppendAllText(strTrainCSVPath, sbContent.ToString());

                        //For Val folder                        
                        sbContent = new StringBuilder();
                        for (int count = nSplitCount[0]; count < nSplitCount[0] + nSplitCount[1]; count++)
                        {
                            string sourceFilePath = listFilterdImages[count].ImagePath;
                            string destFilePath = strValImagesPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);

                                string strRegion = "{\"class id\":\"" + index + "\", \"class name\":\"" + curClass + "\"}";
                                string strAttributes = listFilterdImages[count].ImageName + "," + listFilterdImages[count].RegionCount.ToString() + ",\"" + listFilterdImages[count].Coordinates.Replace("\"", "\"\"") + "\",\"" + strRegion.Replace("\"", "\"\"") + "\"";
                                sbContent.AppendLine(strAttributes);
                            }
                            catch { }
                        }
                        File.AppendAllText(strValCSVPath, sbContent.ToString());

                        //For Test set folder
                        sbContent = new StringBuilder();
                        for (int count = nSplitCount[0] + nSplitCount[1]; count < listFilterdImages.Count; count++)
                        {
                            string sourceFilePath = listFilterdImages[count].ImagePath;
                            string destFilePath = strTestSetPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);

                                string strRegion = "{\"class id\":\"" + index + "\", \"class name\":\"" + curClass + "\"}";
                                string strAttributes = listFilterdImages[count].ImageName + "," + listFilterdImages[count].RegionCount.ToString() + ",\"" + listFilterdImages[count].Coordinates.Replace("\"", "\"\"") + "\",\"" + strRegion.Replace("\"", "\"\"") + "\"";
                                sbContent.AppendLine(strAttributes);
                            }
                            catch { }
                        }
                        File.AppendAllText(strTestCSVPath, sbContent.ToString());
                    }

                    //For Source Folder
                    if (bIsSourceCopy)
                    {
                        for (int count = 0; count < listImagePath.Count; count++)
                        {
                            string sourceFilePath = listImagePath[count];
                            string destFilePath = strSourceDataSetPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            }
                            catch { }
                        }
                    }
                }
                else if (app.settings.ClassType == EnumClassType.Segregation)
                {
                    for (int count = 0; count < app.settings.dictEVSupervisorClass.Count; count++)
                    {
                        string curClass = app.settings.dictEVSupervisorClass.ElementAt(count).Value;
                        string ClassAlias = curClass.Split('(', ')').Length > 1 ? curClass.Split('(', ')')[1] : curClass.Split('(', ')')[0];
                        if (!listClass.Any(s => s.ToUpper() == ClassAlias.ToUpper()))
                            continue;

                        string strClassName = curClass.Split('(', ')').Length > 0 ? curClass.Split('(', ')')[0] : "";
                        listClassNames.Add(strClassName);

                        var listFilterdImages = listLabelledImages.SelectMany(item => item.ImageBox.ListImageClass.Where(menu => menu.ClassAlias.ToUpper() == ClassAlias.ToUpper()).
                                                Select(menu => new
                                                {
                                                    ImagePath = item.ImagePath,
                                                    ImageName = item.ImageBox.ImageBoxName,
                                                    RegionCount = item.ImageBox.ListImageClass.Count,
                                                    ClassAlias = menu.ClassAlias,
                                                    Coordinates = menu.ShapeCoordinates
                                                })).ToList();
                        if (listFilterdImages.Count == 0)
                            continue;

                        string strTrainClassPath = strTrainDataSetPath + @"\" + strClassName;
                        string strValClassPath = strValDataSetPath + @"\" + strClassName;
                        string strTestClassPath = strTestSetPath + @"\" + strClassName;
                        if (!Directory.Exists(strTrainClassPath))
                            Directory.CreateDirectory(strTrainClassPath);
                        if (!Directory.Exists(strValClassPath))
                            Directory.CreateDirectory(strValClassPath);
                        if (!Directory.Exists(strTestClassPath))
                            Directory.CreateDirectory(strTestClassPath);

                        int[] nSplitCount = new int[3];
                        nSplitCount[0] = Convert.ToInt32(Math.Round((listFilterdImages.Count * splitPercent[0]) * 1.0 / 100, 0));
                        nSplitCount[1] = Convert.ToInt32(Math.Round((listFilterdImages.Count * splitPercent[1]) * 1.0 / 100, 0));
                        nSplitCount[2] = listFilterdImages.Count - (nSplitCount[0] + nSplitCount[1]);

                        if (nSplitCount[2] > nSplitCount[1])
                        {
                            int temp = nSplitCount[2];
                            nSplitCount[2] = nSplitCount[1];
                            nSplitCount[1] = temp;
                        }

                        if (nSplitCount[2] < 0)
                            nSplitCount[2] = 0;

                        nTotalSplitDataCount[0] += nSplitCount[0];
                        nTotalSplitDataCount[1] += nSplitCount[1];
                        nTotalSplitDataCount[2] += nSplitCount[2];
                        //For Train folder                    
                        for (int cnt = 0; cnt < nSplitCount[0]; cnt++)
                        {
                            string sourceFilePath = listFilterdImages[cnt].ImagePath;
                            string destFilePath = strTrainClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            }
                            catch { }
                        }

                        //For Validation folder                    
                        for (int cnt = nSplitCount[0]; cnt < nSplitCount[0] + nSplitCount[1]; cnt++)
                        {
                            string sourceFilePath = listFilterdImages[cnt].ImagePath;
                            string destFilePath = strValClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            }
                            catch { }
                        }

                        //For Test set folder                    
                        for (int cnt = nSplitCount[0] + nSplitCount[1]; cnt < listFilterdImages.Count; cnt++)
                        {
                            string sourceFilePath = listFilterdImages[cnt].ImagePath;
                            string destFilePath = strTestClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                            }
                            catch { }
                        }

                        //For Source Folder
                        if (bIsSourceCopy)
                        {
                            string strSourceClassPath = strSourceDataSetPath + @"\" + strClassName;
                            if (!Directory.Exists(strSourceClassPath))
                                Directory.CreateDirectory(strSourceClassPath);

                            for (int cnt = 0; cnt < listFilterdImages.Count; cnt++)
                            {
                                string sourceFilePath = listFilterdImages[cnt].ImagePath;
                                string destFilePath = strSourceClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                                try
                                {
                                    if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                                        Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                                }
                                catch { }
                            }
                        }
                        ////For Train folder
                        //List<string> listClass = app.settings.dictEVSupervisorClass.Values.ToList();
                        //List<List<ImageMenu>> listClasswiseImages = new List<List<ImageMenu>>();
                        //List<string> listFilteredClass = new List<string>();
                        //foreach (string curClass in listClass)
                        //{
                        //    string ClassAlias = curClass.Split('(', ')').Length > 1 ? curClass.Split('(', ')')[1] : curClass.Split('(', ')')[0];
                        //    List<ImageMenu> tempList = listLabelledImages.Where(item => item.ImageBox.ListImageClass.ToList().Exists(s => s.ClassAlias == ClassAlias)).ToList();
                        //    if (tempList.Count > 0)
                        //    {
                        //        listClasswiseImages.Add(tempList);
                        //        string strClass = curClass.Split('(', ')').Length > 0 ? curClass.Split('(', ')')[0] : "";
                        //        listFilteredClass.Add(strClass);
                        //    }
                        //}
                        //for (int count = 0; count < listClasswiseImages.Count; count++)
                        //{
                        //    string strClassName = listFilteredClass[count];
                        //    string strTrainClassPath = strTrainDataSetPath + @"\" + strClassName;
                        //    string strValClassPath = strValDataSetPath + @"\" + strClassName;
                        //    string strTestClassPath = strTestSetPath + @"\" + strClassName;
                        //    if (!Directory.Exists(strTrainClassPath))
                        //        Directory.CreateDirectory(strTrainClassPath);
                        //    if (!Directory.Exists(strValClassPath))
                        //        Directory.CreateDirectory(strValClassPath);
                        //    if (!Directory.Exists(strTestClassPath))
                        //        Directory.CreateDirectory(strTestClassPath);

                        //    List<ImageMenu> listTempImages = listClasswiseImages[count];
                        //    int[] nSplitCount = new int[3];
                        //    nSplitCount[0] = Convert.ToInt32(Math.Round((listTempImages.Count * splitPercent[0]) * 1.0 / 100, 0));
                        //    nSplitCount[1] = Convert.ToInt32(Math.Round((listTempImages.Count * splitPercent[1]) * 1.0 / 100, 0));
                        //    nSplitCount[2] = listTempImages.Count - (nSplitCount[0] + nSplitCount[1]);

                        //    if (nSplitCount[2] > nSplitCount[1])
                        //    {
                        //        int temp = nSplitCount[2];
                        //        nSplitCount[2] = nSplitCount[1];
                        //        nSplitCount[1] = temp;
                        //    }

                        //    if (nSplitCount[2] < 0)
                        //        nSplitCount[2] = 0;

                        //    //For Train folder                    
                        //    for (int cnt = 0; cnt < nSplitCount[0]; cnt++)
                        //    {
                        //        ImageMenu curImage = listTempImages[cnt];
                        //        string sourceFilePath = curImage.ImagePath;
                        //        string destFilePath = strTrainClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                        //        try
                        //        {
                        //            if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                        //                Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                        //        }
                        //        catch { }
                        //    }

                        //    //For Validation folder                    
                        //    for (int cnt = nSplitCount[0]; cnt < nSplitCount[0] + nSplitCount[1]; cnt++)
                        //    {
                        //        ImageMenu curImage = listTempImages[cnt];
                        //        string sourceFilePath = curImage.ImagePath;
                        //        string destFilePath = strValClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                        //        try
                        //        {
                        //            if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                        //                Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                        //        }
                        //        catch { }
                        //    }

                        //    //For Test set folder                    
                        //    for (int cnt = nSplitCount[0] + nSplitCount[1]; cnt < listTempImages.Count; cnt++)
                        //    {
                        //        ImageMenu curImage = listTempImages[cnt];
                        //        string sourceFilePath = curImage.ImagePath;
                        //        string destFilePath = strTestClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);
                        //        try
                        //        {
                        //            if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                        //                Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                        //        }
                        //        catch { }
                        //    }

                        //    //For Source Folder
                        //    if (bIsSourceCopy)
                        //    {
                        //        string strSourceClassPath = strSourceDataSetPath + @"\" + strClassName;
                        //        if (!Directory.Exists(strSourceClassPath))
                        //            Directory.CreateDirectory(strSourceClassPath);

                        //        for (int cnt = 0; cnt < listTempImages.Count; cnt++)
                        //        {
                        //            ImageMenu curImage = listTempImages[cnt];
                        //            string sourceFilePath = curImage.ImagePath;
                        //            string destFilePath = strSourceClassPath + "\\" + System.IO.Path.GetFileName(sourceFilePath);

                        //            try
                        //            {
                        //                if (!Alphaleonis.Win32.Filesystem.File.Exists(destFilePath))
                        //                    Alphaleonis.Win32.Filesystem.File.Copy(sourceFilePath, destFilePath, true);
                        //            }
                        //            catch { }
                        //        }
                        //    }
                    }
                }

                File.WriteAllLines(strClassNametxtPath, listClassNames);
                //app.OnWorkerMethodComplete("complete");
                app.UpdateAugmentationClassData();
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show("Train/Val/test Dataset split operation completed successfully.\nBelow is the DataSplit Count :" +
                                            "\nTrain Data : " + nTotalSplitDataCount[0] + "\nValidation Data : " + nTotalSplitDataCount[1] +
                                            "\nTest Data : " + nTotalSplitDataCount[2], "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
                });
                Utilities.LogMessage("Train/Val/test Dataset split operation completed successfully.", 0);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                //app.OnWorkerMethodComplete("complete");
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                                        MessageBoxImage.Error, MessageBoxResult.None);
                });
            }

            catch (Exception ex)
            {
                //app.OnWorkerMethodComplete("complete");
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show("Something went wrong..!\n" + ex.Message, "Data spilt failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                });
                Utilities.LogMessage("DataFolderSplitter::SplitDataFolderintoParts: " + ex.Message, 0);
            }
            finally
            {
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                });
            }
        }

        public void GenerateClassModelini()
        {
            try
            {
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                if (!Directory.Exists(app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\releasedModels"))
                    Directory.CreateDirectory(app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\releasedModels");

                string modelClassiniPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + strProjectname + "_" + app.settings.Architecture + ".ini";
                if (File.Exists(modelClassiniPath))
                    File.Delete(modelClassiniPath);

                if (app.settings.ClassType == EnumClassType.Segregation)
                {
                    IniFile iniModelFile = new IniFile(modelClassiniPath);
                    iniModelFile.WriteValue("ClassificationModelInfo", "Station", app.settings.Station);
                    iniModelFile.WriteValue("ClassificationModelInfo", "Engineer", "");
                    iniModelFile.WriteValue("ClassificationModelInfo", "ModelRef", app.settings.Architecture);
                    iniModelFile.WriteValue("ClassificationModelInfo", "Date", DateTime.Now.Date.ToString("dd-MM-yyyy"));
                    iniModelFile.WriteValue("ClassificationModelInfo", "Classes", app.settings.dictEVSupervisorClass.Count);
                    iniModelFile.WriteValue("ClassificationModelInfo", "Epochs", "");
                    iniModelFile.WriteValue("ClassificationModelInfo", "Version", "1.0.0.0");
                    iniModelFile.WriteValue("ClassificationModelInfo", "IlluminationType", app.settings.PatchcoreIlluminationType);

                    iniModelFile.WriteValue("ClassificationParameter", "Mean", "");
                    iniModelFile.WriteValue("ClassificationParameter", "ImageWidth", "");
                    iniModelFile.WriteValue("ClassificationParameter", "ImageHeight", "");
                    iniModelFile.WriteValue("ClassificationParameter", "InputLayer", "");
                    iniModelFile.WriteValue("ClassificationParameter", "OutputLayer0", "");

                    foreach (KeyValuePair<int, string> curClass in app.settings.dictEVSupervisorClass)
                    {
                        string ClassName = curClass.Value.Split('(', ')').Length > 0 ? curClass.Value.Split('(', ')')[0] : "";
                        string ClassAlias = curClass.Value.Split('(', ')').Length > 1 ? curClass.Value.Split('(', ')')[1] : curClass.Value.Split('(', ')')[0];
                        iniModelFile.WriteValue("ClassificationClass", "C" + curClass.Key, ClassAlias);
                        iniModelFile.WriteValue("ClassificationClassFullName", ClassAlias, ClassName);

                        if (app.settings.ListFailClass.Contains(ClassAlias))
                            iniModelFile.WriteValue("ClassificationFailClasses", "C" + curClass.Key, ClassAlias);
                        if (app.settings.ListPassClass.Contains(ClassAlias))
                            iniModelFile.WriteValue("ClassificationPassClasses", "C" + curClass.Key, ClassAlias);

                        iniModelFile.WriteValue("ClassificationClassWiseThreshold", ClassAlias, 0.8);
                    }
                }
                else if (app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline)
                {
                    IniFile iniModelFile = new IniFile(modelClassiniPath);
                    iniModelFile.WriteValue("DetectionModelInfo", "Station", app.settings.Station);
                    iniModelFile.WriteValue("DetectionModelInfo", "Engineer", "");
                    iniModelFile.WriteValue("DetectionModelInfo", "ModelRef", app.settings.Architecture);
                    iniModelFile.WriteValue("DetectionModelInfo", "Date", DateTime.Now.Date.ToString("dd-MM-yyyy"));
                    iniModelFile.WriteValue("DetectionModelInfo", "Classes", app.settings.dictEVSupervisorClass.Count);
                    iniModelFile.WriteValue("DetectionModelInfo", "Epochs", "");
                    iniModelFile.WriteValue("DetectionModelInfo", "Version", "1.0.0.1");

                    iniModelFile.WriteValue("DetectionParameter", "Scale", "");
                    iniModelFile.WriteValue("DetectionParameter", "ImageWidth", "");
                    iniModelFile.WriteValue("DetectionParameter", "ImageHeight", "");
                    iniModelFile.WriteValue("DetectionParameter", "InputLayer", "");
                    for (int i = 0; i < 3; i++)
                        iniModelFile.WriteValue("DetectionParameter", "OutputLayer" + i.ToString(), "");

                    foreach (KeyValuePair<int, string> curClass in app.settings.dictEVSupervisorClass)
                    {
                        string ClassName = curClass.Value.Split('(', ')').Length > 0 ? curClass.Value.Split('(', ')')[0] : "";
                        string ClassAlias = curClass.Value.Split('(', ')').Length > 1 ? curClass.Value.Split('(', ')')[1] : curClass.Value.Split('(', ')')[0];
                        iniModelFile.WriteValue("DetectionClass", "C" + curClass.Key, ClassAlias);
                        iniModelFile.WriteValue("DetectionClassFullName", ClassAlias, ClassName);

                        if (app.settings.ListFailClass.Contains(ClassAlias))
                            iniModelFile.WriteValue("DetectionFailClasses", "C" + curClass.Key, ClassAlias);
                        if (app.settings.ListPassClass.Contains(ClassAlias))
                            iniModelFile.WriteValue("DetectionPassClasses", "C" + curClass.Key, ClassAlias);

                        iniModelFile.WriteValue("DetectionClassWiseThreshold", ClassAlias, 0.10);
                    }

                    iniModelFile.WriteValue("DetectionThresholds", "Score", "");
                    iniModelFile.WriteValue("DetectionThresholds", "NMS", "");
                }
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("DataFolderSplitter::GenerateClassModelini: " + ex.Message, 9);
            }
        }

        //private void cmbTrain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (cmbTrain.SelectedIndex == -1)
        //        return;

        //    int nTrainVal = (int)cmbTrain.SelectedValue;
        //    int remainVal = 100 - nTrainVal;
        //    int nValidation = Convert.ToInt16(Math.Round((remainVal * 1.0) / 2.0, 0));
        //    txtVal.Text = nValidation.ToString();
        //    txtTest.Text = (100 - (nTrainVal + nValidation)).ToString();

        //    splitPercent[0] = cmbTrain.SelectedIndex == -1 ? 0 : nTrainVal;
        //    splitPercent[1] = txtVal.Text == string.Empty ? 0 : Convert.ToInt16(txtVal.Text);
        //    splitPercent[2] = txtTest.Text == string.Empty ? 0 : Convert.ToInt16(txtTest.Text);

        //    int TotalCount = int.TryParse(lblRegionCount.Content.ToString(), out int a) ? a : 0;
        //    int[] nSplitCount = new int[3];
        //    nSplitCount[0] = Convert.ToInt32(Math.Round((TotalCount * splitPercent[0]) * 1.0 / 100, 0));
        //    nSplitCount[1] = Convert.ToInt32(Math.Round((TotalCount * splitPercent[1]) * 1.0 / 100, 0));
        //    nSplitCount[2] = TotalCount - (nSplitCount[0] + nSplitCount[1]);

        //    if (nSplitCount[2] > nSplitCount[1])
        //    {
        //        int temp = nSplitCount[2];
        //        nSplitCount[2] = nSplitCount[1];
        //        nSplitCount[1] = temp;
        //    }

        //    if (nSplitCount[2] < 0)
        //        nSplitCount[2] = 0;

        //    txtTrainData.Text = nSplitCount[0].ToString();
        //    txtValData.Text = nSplitCount[1].ToString();
        //    txtTestData.Text = nSplitCount[2].ToString();
        //}

        private void cmbTrain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTrain.SelectedIndex == -1)
                return;

            int nTrainVal = (int)cmbTrain.SelectedValue;

            if (bIsPatchCore)
            {
                // ✅ Only Train & Test
                int nTest = 100 - nTrainVal;

                txtVal.Text = "0"; // not used
                txtTest.Text = nTest.ToString();

                splitPercent[0] = nTrainVal; // Train
                splitPercent[1] = 0;         // Validation
                splitPercent[2] = nTest;     // Test
            }
            else
            {
                // ✅ Existing logic (Train / Val / Test)
                int remainVal = 100 - nTrainVal;
                int nValidation = Convert.ToInt16(Math.Round((remainVal * 1.0) / 2.0, 0));

                txtVal.Text = nValidation.ToString();
                txtTest.Text = (100 - (nTrainVal + nValidation)).ToString();

                splitPercent[0] = nTrainVal;
                splitPercent[1] = nValidation;
                splitPercent[2] = 100 - (nTrainVal + nValidation);
            }

            // ✅ Count calculation (works for both modes)
            int TotalCount = int.TryParse(lblRegionCount.Content.ToString(), out int a) ? a : 0;

            int[] nSplitCount = new int[3];
            nSplitCount[0] = Convert.ToInt32(Math.Round((TotalCount * splitPercent[0]) / 100.0));
            nSplitCount[1] = Convert.ToInt32(Math.Round((TotalCount * splitPercent[1]) / 100.0));
            nSplitCount[2] = TotalCount - (nSplitCount[0] + nSplitCount[1]);

            if (nSplitCount[2] < 0)
                nSplitCount[2] = 0;

            txtTrainData.Text = nSplitCount[0].ToString();
            txtValData.Text = nSplitCount[1].ToString(); // will be 0 for PatchCore
            txtTestData.Text = nSplitCount[2].ToString();
        }

        //private void txtSplit_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if(txtFolder1.Text.Length > 2)
        //    {
        //        e.Handled = true;
        //        return;
        //    }

        //    int nSplit1 = txtFolder1.Text == string.Empty ? 0 : Convert.ToInt16(txtFolder1.Text);

        //    txtFolder2.Text = (100 - nSplit1).ToString();

        //    if (nSplit1 == 0)
        //        txtFolder2.Text = string.Empty;
        //}

        // ─────────────────────────────────────────────────────────────────────
        // PatchCore grid helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the DataGrid with per-class image counts, tagging each class
        /// as "Good" (pass class) or "Defect" (fail class).
        /// </summary>
        private void LoadPatchcoreClassSummary()
        {
            try
            {
                var items = new List<PatchcoreClassRow>();
                var labelledImages = app.ImageMenuList
                    .Where(item => item.ImageBox.ListImageClass.Count > 0
                                   && item.MenuItemBrush != app.ImageMenuBrushes[2])
                    .ToList();

                foreach (var kvp in app.settings.dictEVSupervisorClass)
                {
                    string fullName = kvp.Value;
                    string alias = fullName.Split(new char[] { '(', ')' }).Length > 1
                                       ? fullName.Split(new char[] { '(', ')' })[1]
                                       : fullName.Split(new char[] { '(', ')' })[0];
                    string className = fullName.Split(new char[] { '(', ')' })[0];

                    int count = labelledImages
                        .SelectMany(img => img.ImageBox.ListImageClass
                            .Where(c => c.ClassAlias.ToUpper() == alias.ToUpper()))
                        .Count();

                    bool isDefect = app.settings.ListFailClass
                        .Any(fc => fc.ToUpper() == alias.ToUpper());
                    bool isGood = app.settings.ListPassClass
                        .Any(pc => pc.ToUpper() == alias.ToUpper());

                    string classType = isDefect ? "Defect" : (isGood ? "Good" : "Other");

                    items.Add(new PatchcoreClassRow
                    {
                        ClassName = string.IsNullOrWhiteSpace(className) ? alias : className,
                        ClassType = classType,
                        ImageCount = count
                    });
                }

                dgPatchcoreClasses.ItemsSource = items;
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("DataFolderSplitter::LoadPatchcoreClassSummary: " + ex.Message, 9);
            }
        }

        /// <summary>
        /// Recalculates and updates the Train/Test count labels based on the slider value.
        /// Good images: trainPct% -> Train, rest -> Test.
        /// Defect images: 100% -> Test.
        /// </summary>
        private void UpdatePatchcoreSplitCounts()
        {
            try
            {
                int trainPct = (int)(sldrGoodTrainSplit.Value);
                int testPct = 100 - trainPct;

                lblGoodTrainPct.Content = trainPct + "%";
                lblGoodTestPct.Content = testPct + "% Test";

                lblPatchcoreSplitInfo.Text =
                    "Defect images will move to Test folder.  " +
                    "Good images will move " + trainPct + "% to Train folder and " + testPct + "% to Test folder.";

                // Count good and defect images
                var labelledImages = app.ImageMenuList
                    .Where(item => item.ImageBox.ListImageClass.Count > 0
                                   && item.MenuItemBrush != app.ImageMenuBrushes[2])
                    .ToList();

                int goodCount = 0;
                int defectCount = 0;

                foreach (var kvp in app.settings.dictEVSupervisorClass)
                {
                    string fullName = kvp.Value;
                    string alias = fullName.Split(new char[] { '(', ')' }).Length > 1
                                      ? fullName.Split(new char[] { '(', ')' })[1]
                                      : fullName.Split(new char[] { '(', ')' })[0];

                    int count = labelledImages
                        .SelectMany(img => img.ImageBox.ListImageClass
                            .Where(c => c.ClassAlias.ToUpper() == alias.ToUpper()))
                        .Count();

                    bool isDefect = app.settings.ListFailClass
                        .Any(fc => fc.ToUpper() == alias.ToUpper());
                    bool isGood = app.settings.ListPassClass
                        .Any(pc => pc.ToUpper() == alias.ToUpper());

                    if (isDefect) defectCount += count;
                    else if (isGood) goodCount += count;
                }

                int goodToTrain = (int)Math.Round(goodCount * trainPct / 100.0);
                int goodToTest = goodCount - goodToTrain;
                int totalTrain = goodToTrain;
                int totalTest = goodToTest + defectCount;

                lblPCTrainCount.Content = totalTrain.ToString();
                lblPCTestCount.Content = totalTest.ToString();

                // Store split info so btnProceed_Click can use it
                splitPercent[0] = trainPct;   // Good -> Train %
                splitPercent[1] = 0;           // No validation for PatchCore
                splitPercent[2] = testPct;     // Good -> Test %  (defect always 100% test)
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("DataFolderSplitter::UpdatePatchcoreSplitCounts: " + ex.Message, 9);
            }
        }

        private void sldrGoodTrainSplit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            UpdatePatchcoreSplitCounts();
        }

        private void btnPatchcoreProceed_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation: only Good-class images are checked ─────────────────
            var labelledImages = app.ImageMenuList
                .Where(item => item.ImageBox.ListImageClass.Count > 0
                               && item.MenuItemBrush != app.ImageMenuBrushes[2])
                .ToList();

            // Count total Good images across all Pass classes
            int nTotalGoodImages = 0;
            foreach (var kvp in app.settings.dictEVSupervisorClass)
            {
                string fullName = kvp.Value;
                string alias = fullName.Split(new char[] { '(', ')' }).Length > 1
                                  ? fullName.Split(new char[] { '(', ')' })[1]
                                  : fullName.Split(new char[] { '(', ')' })[0];

                bool isGood = app.settings.ListPassClass
                    .Any(pc => pc.ToUpper() == alias.ToUpper());

                if (!isGood)
                    continue;   // skip Defect classes entirely

                nTotalGoodImages += labelledImages
                    .SelectMany(img => img.ImageBox.ListImageClass
                        .Where(c => c.ClassAlias.ToUpper() == alias.ToUpper()))
                    .Count();
            }

            // Hard stop: must have at least 10 Good images
            if (nTotalGoodImages < 10)
            {
                System.Windows.MessageBox.Show(
                    "Dataset must contain at least 10 Good segregated images to split Train/Test set.\n" +
                    "Current Good image count : " + nTotalGoodImages + "\n\n" +
                    "Please segregate more Good images and try again.",
                    "Insufficient Good Images", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None);
                return;
            }

            // ── Launch BackgroundWorker (no per-class exclusion needed) ────────
            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            // Pass an empty exclude list — defect images are always included as-is
            app.busyIndicator.IsBusy = true;
            BGWorkerFormat.DoWork += bgwDowork_PatchcoreSplit;
            BGWorkerFormat.RunWorkerAsync(new List<string>());
            this.Close();
        }

        private void bgwDowork_PatchcoreSplit(object sender, DoWorkEventArgs e)
        {
            List<string> listExcludeClass = e.Argument as List<string>;
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadSplit = new Thread(() => SplitDataFolderPatchcore(listExcludeClass));
                threadSplit.IsBackground = true;
                threadSplit.Start();
            }
        }

        /// <summary>
        /// PatchCore-specific folder split.
        ///
        /// Folder structure created:
        ///   Train        ///       Good\          <- trainPct% of Good-class images
        ///   Test        ///       Good\          <- remaining Good images (testPct%)
        ///       &lt;DefectClass&gt;\  <- 100% of each Defect-class images (per class sub-folder)
        /// </summary>
        private void SplitDataFolderPatchcore(List<string> listExcludeAlias)
        {
            try
            {
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject)
                                        ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                string strTrainDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.trainFolder;
                string strTestSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.testsetFolder;
                string strSourceDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.sourceFolder;

                // Guard: images must not already be loaded from these paths
                if (app.settings.LoadImagePath.Contains(strTrainDataSetPath) ||
                    app.settings.LoadImagePath.Contains(strTestSetPath) ||
                    app.settings.LoadImagePath.Contains(strSourceDataSetPath))
                {
                    Dispatcher.Invoke(() => 
                    {
                        app.busyIndicator.IsBusy = false;
                        System.Windows.MessageBox.Show("Error while Data-split operation! Images are loaded from train/test/source path.Cannot continue." +
                                                    "Load images from a different path and retry.",
                                                    "Abort", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                        Utilities.LogMessage("PatchCore DataSplit aborted: images loaded from output paths.", 0);
                    });
                    return;
                }

                // ── Disk-space check ─────────────────────────────────────────
                List<string> listImagePath = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0
                                    && item.MenuItemBrush != app.ImageMenuBrushes[2]).Select(item => item.ImagePath).ToList();

                if (!app.IsSegregationDiskSpaceOK(app.ConfigFilePath, listImagePath))
                {
                    Dispatcher.Invoke(() => {
                        app.busyIndicator.IsBusy = false;
                        System.Windows.MessageBox.Show(
                            "Output disk is full. Cannot copy images. Free some space and try again.",
                            "No Storage Space", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                    });
                    Utilities.LogMessage("PatchCore DataSplit aborted: insufficient disk space.", 0);
                    return;
                }

                // ── Recreate output folders ───────────────────────────────────
                if (Directory.Exists(strTrainDataSetPath))
                    Directory.Delete(strTrainDataSetPath, true);
                Directory.CreateDirectory(strTrainDataSetPath);

                if (Directory.Exists(strTestSetPath))
                    Directory.Delete(strTestSetPath, true);
                Directory.CreateDirectory(strTestSetPath);

                if (Directory.Exists(strSourceDataSetPath))
                    Directory.Delete(strSourceDataSetPath, true);
                Directory.CreateDirectory(strSourceDataSetPath);

                // Train\Good  and  Test\Good  sub-folders
                string strTrainGoodPath = System.IO.Path.Combine(strTrainDataSetPath, "Good");
                string strTestGoodPath = System.IO.Path.Combine(strTestSetPath, "Good");
                Directory.CreateDirectory(strTrainGoodPath);
                Directory.CreateDirectory(strTestGoodPath);

                bool bIsSourceCopy = true;
                Dispatcher.Invoke(() => bIsSourceCopy = chkCopySource.IsChecked.Value);

                int trainPct = splitPercent[0];   // Good images -> Train %
                int nTrainTotal = 0, nTestTotal = 0;
                List<string> listClassNames = new List<string>();

                var labelledImages = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0
                                   && item.MenuItemBrush != app.ImageMenuBrushes[2]).ToList();

                // ── Process each class ────────────────────────────────────────
                foreach (var kvp in app.settings.dictEVSupervisorClass)
                {
                    string fullName = kvp.Value;
                    string alias = fullName.Split(new char[] { '(', ')' }).Length > 1
                                       ? fullName.Split(new char[] { '(', ')' })[1]
                                       : fullName.Split(new char[] { '(', ')' })[0];
                    string className = fullName.Split(new char[] { '(', ')' })[0].Trim();
                    if (string.IsNullOrWhiteSpace(className)) className = alias;

                    // Skip classes the user chose to exclude
                    if (listExcludeAlias.Any(s => s.ToUpper() == alias.ToUpper()))
                        continue;

                    bool isGood = app.settings.ListPassClass.Any(pc => pc.ToUpper() == alias.ToUpper());
                    bool isDefect = app.settings.ListFailClass.Any(fc => fc.ToUpper() == alias.ToUpper());

                    // Gather all segregated images for this class
                    var classImages = labelledImages.SelectMany(item => item.ImageBox.ListImageClass
                            .Where(menu => menu.ClassAlias.ToUpper() == alias.ToUpper()).Select(menu => item.ImagePath)).Distinct().ToList();

                    if (classImages.Count == 0)
                        continue;

                    listClassNames.Add(className);

                    if (isGood)
                    {
                        // ── Good class: split by slider ratio ────────────────
                        //    trainPct% -> Train\Good                        //    rest      -> Test\Good
                        int nToTrain = (int)Math.Round(classImages.Count * trainPct / 100.0);
                        int nToTest = classImages.Count - nToTrain;

                        for (int i = 0; i < nToTrain; i++)
                        {
                            string src = classImages[i];
                            string dest = System.IO.Path.Combine(strTrainGoodPath, System.IO.Path.GetFileName(src));
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(dest))
                                    Alphaleonis.Win32.Filesystem.File.Copy(src, dest, true);
                            }
                            catch { }
                        }

                        for (int i = nToTrain; i < classImages.Count; i++)
                        {
                            string src = classImages[i];
                            string dest = System.IO.Path.Combine(strTestGoodPath, System.IO.Path.GetFileName(src));
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(dest))
                                    Alphaleonis.Win32.Filesystem.File.Copy(src, dest, true);
                            }
                            catch { }
                        }

                        nTrainTotal += nToTrain;
                        nTestTotal += nToTest;
                    }
                    else if (isDefect)
                    {
                        // ── Defect class: 100% -> Test\<ClassName>\ ──────────
                        string strTestDefectClassPath = System.IO.Path.Combine(strTestSetPath, className);
                        if (!Directory.Exists(strTestDefectClassPath))
                            Directory.CreateDirectory(strTestDefectClassPath);

                        foreach (string src in classImages)
                        {
                            string dest = System.IO.Path.Combine(strTestDefectClassPath, System.IO.Path.GetFileName(src));
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(dest))
                                    Alphaleonis.Win32.Filesystem.File.Copy(src, dest, true);
                            }
                            catch { }
                        }

                        nTestTotal += classImages.Count;
                    }

                    // ── Source folder copy (all images, flat) ─────────────────
                    if (bIsSourceCopy)
                    {
                        string strSourceClassPath = System.IO.Path.Combine(strSourceDataSetPath, className);
                        if (!Directory.Exists(strSourceClassPath))
                            Directory.CreateDirectory(strSourceClassPath);

                        foreach (string src in classImages)
                        {
                            string dest = System.IO.Path.Combine(strSourceClassPath, System.IO.Path.GetFileName(src));
                            try
                            {
                                if (!Alphaleonis.Win32.Filesystem.File.Exists(dest))
                                    Alphaleonis.Win32.Filesystem.File.Copy(src, dest, true);
                            }
                            catch { }
                        }
                    }
                }

                GenerateClassModelini();
                Dispatcher.Invoke(() =>
                {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show(
                        "Train/Test dataset split completed successfully.\n\n" +
                        "Train Data (Good)  : " + nTrainTotal + "\n" +
                        "Test  Data (Total) : " + nTestTotal,
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
                });
                Utilities.LogMessage("Anomay detection Train/Test dataset split completed successfully. Train: " + nTrainTotal + "  Test: " + nTestTotal, 0);
            }
            catch (Exception ex) when(ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show(
                        "The specified output path or file name is too long.\nPlease set a shorter Output Data Path in Settings.",
                        "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => {
                    app.busyIndicator.IsBusy = false;
                    System.Windows.MessageBox.Show(
                        "Something went wrong!\n" + ex.Message,
                        "PatchCore split failed", MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                });
                Utilities.LogMessage("DataFolderSplitter::SplitDataFolderPatchcore: " + ex.Message, 0);
            }
            finally
            {
                Dispatcher.Invoke(() => app.busyIndicator.IsBusy = false);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data model for PatchCore class summary DataGrid
    // ─────────────────────────────────────────────────────────────────────────
    public class PatchcoreClassRow
{
    public string ClassName { get; set; }
    public string ClassType { get; set; }
    public int ImageCount { get; set; }
}
}