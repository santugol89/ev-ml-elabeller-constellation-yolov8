using AForge.Imaging.Filters;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using AForge.Math;
using System.Text.RegularExpressions;
using GenieSupervisor.Data_Augmentation;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public List<AugmentTypeClass> ListAugmentTypeClass = new List<AugmentTypeClass>();
        DateTime dtLastAugmentTime = new DateTime();

        private void btnAugment_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageMenuList == null || ImageMenuList.Count == 0){
                System.Windows.MessageBox.Show("Please Load Images to do Augmentation..!", "Images not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            if (!CheckCSVFileLoaded())
                return;

            ListAugmentTypeClass.ForEach(item => item.AugmentExportCount = 0);
            ListAugmentTypeClass.ForEach(item => item.AugmentStatCount = 0);
            ListAugmentTypeClass.ForEach(item => item.AugmentTypestats = new AugmentTypeStat());
            List<string> listAugmentTypePool = GetAugmentTypePool();

            if (radNormal.IsChecked.Value && ListAugmentTypeClass.Where(item => item.IsSelectChecked).Count() == 0){
                System.Windows.MessageBox.Show("No Augmentation types has been selected to proceed..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (radRandom.IsChecked.Value)
            {
                if (listAugmentTypePool.Count == 0){
                    System.Windows.MessageBox.Show("Please select Augmetation type..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (string.IsNullOrEmpty(txtBatchSize.Text.Trim()) || Convert.ToInt32(txtBatchSize.Text.Trim()) == 0){
                    System.Windows.MessageBox.Show("Batch size cannot be blank/zero..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (Convert.ToInt32(txtBatchSize.Text.Trim()) > SourceTotalCount){
                    System.Windows.MessageBox.Show("Batch size cannot be greater than source count..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            char type = radNormal.IsChecked.Value ? 'N' : 'R';
            object[] arrArgs;
            if (radNormal.IsChecked.Value)
                arrArgs = new object[] { type };
            else
                arrArgs = new object[] { type, listAugmentTypePool };

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_ImageAugmentationProcess;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(arrArgs);
            OnWorkerMethodStart_withPercentage();
        }

        public void RefreshAugmentationClassList()
        {
            List<ClassStats> listClassStats = GetClassFolderStatistics();
            foreach (ClassStats curClassStat in listClassStats)
            {
                var curItem = ListAugmentTypeClass.FirstOrDefault(item => item.AugmentClassStat.AliasName == curClassStat.AliasName);
                if (curItem != null && curItem.AugmentClassStat.Count != curClassStat.Count)
                {
                    curItem.AugmentClassStat = curClassStat;
                    curItem.TargetCount = curClassStat.Count;
                }
            }

            this.Dispatcher.Invoke(() => {
                ListAugmentationView.Items.Refresh();
                NotifyPropertyChanged("SourceTotalCount");
                if (ListAugmentationView.Items.Count > 0)
                    lblAugmentStatus.Content = dtLastAugmentTime == new DateTime() ? "Last Augmentation process : Never" : "Last Augmentation process : " + dtLastAugmentTime.ToShortDateString() + " " + dtLastAugmentTime.ToShortTimeString();
            });
        }

        private List<string> GetAugmentTypePool()
        {
            List<string> tempList = new List<string>();
            if (radNormal.IsChecked.Value)
                return tempList;

            if (tgAH.IsChecked.Value)
                tempList.Add("AH");
            if (tgAV.IsChecked.Value)
                tempList.Add("AV");
            if (tgAN.IsChecked.Value)
                tempList.Add("AN");
            if (tgAR.IsChecked.Value)
                tempList.Add("AR");
            if (tgAT.IsChecked.Value)
                tempList.Add("AT");
            if (tgAB.IsChecked.Value)
                tempList.Add("AB");

            return tempList;
        }

        public AugmentationConfigWindow _windowAugmentationConfig = null;
        private void ButtonAugmentationConfig_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_windowAugmentationConfig != null)
                _windowAugmentationConfig.Close();

            EnumAugmentionType AugType = (sender as System.Windows.Controls.Label).Uid == "lblNoise" ? EnumAugmentionType.Noise :
                                         (sender as System.Windows.Controls.Label).Uid == "lblRot" ? EnumAugmentionType.Rotate :
                                         (sender as System.Windows.Controls.Label).Uid == "lblTrans" ? EnumAugmentionType.Trans :
                                         (sender as System.Windows.Controls.Label).Uid == "lblBlur" ? EnumAugmentionType.Blur : EnumAugmentionType.None;
            Point p = e.GetPosition(this);

            _windowAugmentationConfig = new AugmentationConfigWindow(this, AugType);
            _windowAugmentationConfig.WindowStartupLocation = WindowStartupLocation.Manual;
            _windowAugmentationConfig.Left = p.X - 400;
            _windowAugmentationConfig.Top = p.Y + 100 < SystemParameters.PrimaryScreenHeight ? p.Y + 20 : p.Y - 100;
            _windowAugmentationConfig.Owner = this;
            _windowAugmentationConfig.Show();
        }

        private void gridMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_windowAugmentationConfig != null)
                _windowAugmentationConfig.Close();
        }

        private Visibility isVisibleAgmentButton = Visibility.Collapsed;
        public Visibility IsVisibleAgmentButton
        {
            get
            {
                return isVisibleAgmentButton;
            }
            set
            {
                isVisibleAgmentButton = value;
                NotifyPropertyChanged("IsVisibleAgmentButton");
            }
        }

        private void chkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (AugmentTypeClass curClass in ListAugmentTypeClass)
            {
                if (curClass.IsTypeEnable)
                {
                    curClass.IsSelectChecked = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
                    System.Windows.Controls.CheckBox chkBox = new System.Windows.Controls.CheckBox();
                    chkBox.DataContext = curClass;
                    chkBox.IsChecked = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
                    chkSelect_Click(chkBox, null);
                }
            }
        }

        private void chkSelect_Click(object sender, RoutedEventArgs e)
        {
            AugmentTypeClass curClass = (sender as System.Windows.Controls.CheckBox).DataContext as AugmentTypeClass;
            curClass.IsHFlipSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsVFlipSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsNoiseSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsRotSelected = false;//(sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
        //    curClass.IsRotSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsTransSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsBlurSelected = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
        }

        public void ImageAugmentationProcess(object[] arrArgs)
        {
            try
            {
                labelEvent.Reset();
                SaveEvent.Reset();
                char Type = (char)arrArgs[0];
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";

                string strDataPath = settings.CSVExportPath + @"\Output Data\Augmentation\" + DateTime.Now.ToString("ddMMyyyy_HHmmss");
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);
                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "augment_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");

                string seperator = ",";
                StringBuilder sbCSVdata = new StringBuilder();
                sbCSVdata.AppendLine(string.Join(seperator, "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                sbCSVdata.AppendLine("filename,region_count,region_shape_attributes,region_attributes,augmentation_type");

                List<string[]> listCSVDataLines = new List<string[]>();
                for (int count = 0; count < ListDatasheetImportData.Count; count++)
                    listCSVDataLines.AddRange(ListDatasheetImportData[count].ListImportData);

                for (int i = 0; i < listCSVDataLines.Count; i++)
                {
                    string[] lineSplit = listCSVDataLines[i].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                    if (!IsValidCSVLine(lineSplit) || IsColHeaderLine(lineSplit))
                    {
                        listCSVDataLines.RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                if (Type == 'N')
                    NormalAugmentProcessandExport(listCSVDataLines, strDataPath, sbCSVdata);
                else
                    RandomAugmentProcessandExport(arrArgs[1] as List<string>, listCSVDataLines, strDataPath, sbCSVdata);

                OnWorkerMethodComplete("Complete");
                NotifyPropertyChanged("AugmentExportCount");
                if (AugmentExportCount == 0)
                {
                    System.Windows.MessageBox.Show("No loaded images matches with import CSV images..", "Augmentation failed", MessageBoxButton.OK,
                            MessageBoxImage.Warning, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    if (Directory.Exists(strDataPath))
                        Directory.Delete(strDataPath, true);
                }
                else if (AugmentExportCount > 0)
                {
                    File.WriteAllText(strCSVSavePath, sbCSVdata.ToString());
                    ExportAugmentationReportToPDF(strDataPath);
                    dtLastAugmentTime = DateTime.Now;
                    Dispatcher.Invoke(() =>
                    {
                        lvStatistics.ItemsSource = ListAugmentTypeClass;
                        lvStatistics.Items.Refresh();
                        lblAugmentStatus.Content = "Last Augmentation process : " + dtLastAugmentTime.ToShortDateString() + " " + dtLastAugmentTime.ToShortTimeString();

                        if (Type == 'N')
                            SaveLastAugmentedStatHistory();
                        else
                            SaveLastAugmentedStatHistory(arrArgs[1] as List<string>);
                    });
                    System.Windows.MessageBox.Show("Augmentated images and Reports are successfully saved in Output folder..", "Success", MessageBoxButton.OK,
                                    MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    Utilities.LogMessage("Augmentation successfully done", 0);
                }
                labelEvent.Set();
                SaveEvent.Set();
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                labelEvent.Set();
                SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                labelEvent.Set();
                SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Something went wrong..!\n" + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("MainWindow::ImageAugmentationProcessandExport: " + ex.Message, 9);
            }
        }

        private void NormalAugmentProcessandExport(List<string[]> listCSVDataLines, string strDataPath, StringBuilder sbCSVdata)
        {
            var tempImageList = ImageMenuList.Where(item => item.MenuItemBrush != ImageMenuBrushes[0]);
            Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = listCSVDataLines.Count);
            for (int lines = 0; lines < listCSVDataLines.Count; lines++)
            {
                Dispatcher.Invoke(() => progressBar.pbStatus.Value = lines);
                string[] lineSplit = listCSVDataLines[lines].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();

                string[] arrShapeAttribute = Regex.Split(lineSplit[2], @",(?=[^\]]*(?:\[|$))");
                if (arrShapeAttribute.Length < 3)
                    continue;

                string strTempShape = arrShapeAttribute[0].Substring(arrShapeAttribute[0].LastIndexOf(':') + 1).ToLower();
                EnumSelectedShape shape = strTempShape.Contains("rect") ? EnumSelectedShape.Rectangle : strTempShape.Contains("poly") ?
                                    EnumSelectedShape.Polyline : strTempShape.Contains("circ") ? EnumSelectedShape.Circle : EnumSelectedShape.Null;

                double X = 0, Y = 0, Width = 0, Height = 0;
                List<double> all_point_x = new List<double>();
                List<double> all_point_y = new List<double>();
                string shapeCoord = "";
                if (shape == EnumSelectedShape.Rectangle || shape == EnumSelectedShape.Circle)
                {
                    X = Convert.ToDouble(arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1));
                    Y = Convert.ToDouble(arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1));
                    Width = Convert.ToDouble(arrShapeAttribute[3].Substring(arrShapeAttribute[3].LastIndexOf(':') + 1));
                    Height = Convert.ToDouble(arrShapeAttribute[4].Substring(arrShapeAttribute[4].LastIndexOf(':') + 1));

                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"x\":" + X + ", \"y\": " + Y +
                                                    ", \"width\": " + Width + ", \"height\": " + Height + " }";
                }

                else if (shape == EnumSelectedShape.Polyline)
                {
                    string subString_x = arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                    all_point_x = subString_x.Split(',').Select(double.Parse).ToList();

                    string subString_y = arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                    all_point_y = subString_y.Split(',').Select(double.Parse).ToList();

                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"all_points_x\": [" +
                                        String.Join(", ", all_point_x) + "], \"all_points_y\": [" + String.Join(", ", all_point_y) + "] }";
                }
                
                string fileName = lineSplit[0].Trim().ToString();
                string regionCount = "1"; // lineSplit[1].Trim().ToString();
                var curImageMenu = tempImageList.FirstOrDefault(temp => temp.ImageName == fileName);
                if (curImageMenu == null)
                    continue;

                string ClassName = Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                AugmentTypeClass curAugClass = ListAugmentTypeClass.FirstOrDefault(item => item.AugmentClassStat.AliasName.ToUpper() == ClassName && item.IsSelectChecked && item.TargetCount > 0);

                if (curAugClass == null)
                    continue;

                curAugClass.AugmentExportCount++;
                if (curAugClass.AugmentExportCount > curAugClass.TargetCount)
                    continue;

                ImageClass curImageClass = null;
                foreach (ImageClass tempImageClass in curImageMenu.ImageBox.ListImageClass){
                    if (tempImageClass.ClassAlias == curAugClass.AugmentClassStat.AliasName && tempImageClass.ShapeCoordinates == shapeCoord)
                    {
                        curImageClass = tempImageClass;
                        break;
                    }
                }

                if (curImageClass == null)
                    continue;

                Dispatcher.Invoke(() =>{
                    BitmapImage bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(curImageMenu.ImagePath)){
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    Shape shapeROI = GetROIShapeForAugmentation(curImageClass);
                    GetAugmentationforImage(curImageClass, bmpImage, strDataPath, fileName, shapeROI, sbCSVdata, regionCount, curAugClass);
                });
            }
        }

        private void RandomAugmentProcessandExport(List<string> listAugTypePool, List<string[]> listCSVDataLines, string strDataPath, StringBuilder sbCSVdata)
        {
            int bBatchProcess = 0;
            while (listCSVDataLines.Count > 0)
            {
                List<string[]> listRandomLines = GetRandomDataSheetLines(listCSVDataLines);

                bBatchProcess++;
                var tempImageList = ImageMenuList.Where(item => item.MenuItemBrush != ImageMenuBrushes[0]);
                Dispatcher.Invoke(() => {
                    progressBar.pbStatus.Maximum = listRandomLines.Count;
                    progressBar.pbStausText.Text = "Processing Batch " + bBatchProcess;
                });
                for (int lines = 0; lines < listRandomLines.Count; lines++)
                {
                    Dispatcher.Invoke(() => progressBar.pbStatus.Value = lines);
                    string[] lineSplit = listRandomLines[lines].Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                    string[] arrShapeAttribute = Regex.Split(lineSplit[2], @",(?=[^\]]*(?:\[|$))");
                    if (arrShapeAttribute.Length < 3)
                        continue;

                    string strTempShape = arrShapeAttribute[0].Substring(arrShapeAttribute[0].LastIndexOf(':') + 1).ToLower();
                    EnumSelectedShape shape = strTempShape.Contains("rect") ? EnumSelectedShape.Rectangle : strTempShape.Contains("poly") ?
                                        EnumSelectedShape.Polyline : strTempShape.Contains("circ") ? EnumSelectedShape.Circle : EnumSelectedShape.Null;

                    double X = 0, Y = 0, Width = 0, Height = 0;
                    List<double> all_point_x = new List<double>();
                    List<double> all_point_y = new List<double>();
                    string shapeCoord = "";
                    if (shape == EnumSelectedShape.Rectangle || shape == EnumSelectedShape.Circle)
                    {
                        X = Convert.ToDouble(arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1));
                        Y = Convert.ToDouble(arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1));
                        Width = Convert.ToDouble(arrShapeAttribute[3].Substring(arrShapeAttribute[3].LastIndexOf(':') + 1));
                        Height = Convert.ToDouble(arrShapeAttribute[4].Substring(arrShapeAttribute[4].LastIndexOf(':') + 1));

                        shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"x\":" + X + ", \"y\": " + Y +
                                                        ", \"width\": " + Width + ", \"height\": " + Height + " }";
                    }

                    else if (shape == EnumSelectedShape.Polyline)
                    {
                        string subString_x = arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                        all_point_x = subString_x.Split(',').Select(double.Parse).ToList();

                        string subString_y = arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                        all_point_y = subString_y.Split(',').Select(double.Parse).ToList();

                        shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"all_points_x\": [" +
                                            String.Join(", ", all_point_x) + "], \"all_points_y\": [" + String.Join(", ", all_point_y) + "] }";
                    }

                    string fileName = lineSplit[0].Trim().ToString();
                    string regionCount = "1"; // lineSplit[1].Trim().ToString();
                    var curImageMenu = tempImageList.FirstOrDefault(temp => temp.ImageName == fileName);
                    if (curImageMenu == null)
                        continue;

                    string ClassName = Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();

                    ImageClass curImageClass = null;
                    foreach (ImageClass tempImageClass in curImageMenu.ImageBox.ListImageClass){
                        if (tempImageClass.ClassAlias.ToUpper() == ClassName && tempImageClass.ShapeCoordinates == shapeCoord)
                        {
                            curImageClass = tempImageClass;
                            break;
                        }
                    }

                    if (curImageClass == null)
                        continue;

                    AugmentTypeClass curAugClass = ListAugmentTypeClass.FirstOrDefault(item => item.AugmentClassStat.AliasName.ToUpper() == ClassName);

                    Dispatcher.Invoke(() =>
                    {
                        BitmapImage bmpImage = new BitmapImage();
                        try
                        {
                            using (FileStream stream = Delimon.Win32.IO.File.OpenRead(curImageMenu.ImagePath))
                            {
                                bmpImage.BeginInit();
                                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                bmpImage.StreamSource = stream;
                                bmpImage.EndInit();
                            }
                        }
                        catch { }

                        Shape shapeROI = GetROIShapeForAugmentation(curImageClass);
                        Random random = new Random();
                        int index = random.Next(listAugTypePool.Count);
                        string strAugType = listAugTypePool[index];
                        UpdateAugmentTypeStat(curAugClass, strAugType);
                        GetAugmentationforImage(curImageClass, bmpImage, strDataPath, fileName, shapeROI, sbCSVdata, regionCount, null, strAugType);
                    });
                }
            }
        }

        private void UpdateAugmentTypeStat(AugmentTypeClass curAugClass, string strAugType)
        {
            curAugClass.AugmentStatCount++;

            if (strAugType.Contains("AH"))
                curAugClass.AugmentTypestats.TypeHorizontalCount++;
            else if (strAugType.Contains("AV"))
                curAugClass.AugmentTypestats.TypeVerticalCount++;
            else if (strAugType.Contains("AN"))
                curAugClass.AugmentTypestats.TypeNoiseCount++;
            else if (strAugType.Contains("AR"))
                curAugClass.AugmentTypestats.TypeRotateCount++;
            else if (strAugType.Contains("AT"))
                curAugClass.AugmentTypestats.TypeTransCount++;
            else if (strAugType.Contains("AB"))
                curAugClass.AugmentTypestats.TypeBlurCount++;
        }

        private List<string[]> GetRandomDataSheetLines(List<string[]> listCSVDataLines)
        {
            List<string[]> tempList = new List<string[]>();
            //comment
            int nBatchSize = 0;
            Dispatcher.Invoke(() => nBatchSize = Convert.ToInt32(txtBatchSize.Text.Trim()));
            Random random = new Random();
            for (int index = 0; index < nBatchSize; index++)
            {
                if (listCSVDataLines.Count < 1)
                    break;
                int rndIndex = random.Next(0, listCSVDataLines.Count - 1);
                tempList.Add(listCSVDataLines[rndIndex]);
                listCSVDataLines.RemoveAt(rndIndex);
            }
            return tempList;
        }

        public System.Drawing.Bitmap GetImagewithShape(System.Drawing.Bitmap tempImage, ImageClass curImageClass)
        {
            System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(tempImage);

            gfx.DrawImage(tempImage, new System.Drawing.Rectangle(0, 0, tempImage.Width, tempImage.Height));
            System.Drawing.Pen a = new System.Drawing.Pen(System.Drawing.Color.Red, ShapeStrokeThickness);
            gfx.DrawRectangle(a, new System.Drawing.Rectangle((int)curImageClass.XCoordinate, (int)curImageClass.YCoordinate, (int)curImageClass.Width, (int)curImageClass.Height));
            gfx.Dispose();

            return tempImage;
        }

        public static System.Drawing.Bitmap RotateImage(System.Drawing.Bitmap bmpSrc, double angle, System.Drawing.Color? extendedBitmapBackground = null)
        {
            System.Drawing.Drawing2D.Matrix mRotate = new System.Drawing.Drawing2D.Matrix();
            mRotate.Translate(bmpSrc.Width / -2, bmpSrc.Height / -2, System.Drawing.Drawing2D.MatrixOrder.Append);
            mRotate.RotateAt((float)angle, new System.Drawing.Point(0, 0), System.Drawing.Drawing2D.MatrixOrder.Append);
            using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath())
            {  // transform image points by rotation matrix
                gp.AddPolygon(new System.Drawing.Point[] { new System.Drawing.Point(0, 0), new System.Drawing.Point(bmpSrc.Width, 0), new System.Drawing.Point(0, bmpSrc.Height) });
                gp.Transform(mRotate);
                System.Drawing.PointF[] pts = gp.PathPoints;

                // create destination bitmap sized to contain rotated source image
                System.Drawing.Rectangle bbox = BoundingBox(bmpSrc, mRotate);
                System.Drawing.Bitmap bmpDest = new System.Drawing.Bitmap(bbox.Width, bbox.Height);

                using (System.Drawing.Graphics gDest = System.Drawing.Graphics.FromImage(bmpDest))
                {
                    if (extendedBitmapBackground != null)
                    {
                        gDest.Clear(extendedBitmapBackground.Value);
                    }
                    // draw source into dest
                    System.Drawing.Drawing2D.Matrix mDest = new System.Drawing.Drawing2D.Matrix();
                    mDest.Translate(bmpDest.Width / 2, bmpDest.Height / 2, System.Drawing.Drawing2D.MatrixOrder.Append);
                    gDest.Transform = mDest;
                    gDest.DrawImage(bmpSrc, pts);
                    return bmpDest;
                }
            }
        }

        private static System.Drawing.Rectangle BoundingBox(System.Drawing.Image img, System.Drawing.Drawing2D.Matrix matrix)
        {
            System.Drawing.GraphicsUnit gu = new System.Drawing.GraphicsUnit();
            System.Drawing.Rectangle rImg = System.Drawing.Rectangle.Round(img.GetBounds(ref gu));

            // Transform the four points of the image, to get the resized bounding box.
            System.Drawing.Point topLeft = new System.Drawing.Point(rImg.Left, rImg.Top);
            System.Drawing.Point topRight = new System.Drawing.Point(rImg.Right, rImg.Top);
            System.Drawing.Point bottomRight = new System.Drawing.Point(rImg.Right, rImg.Bottom);
            System.Drawing.Point bottomLeft = new System.Drawing.Point(rImg.Left, rImg.Bottom);
            System.Drawing.Point[] points = new System.Drawing.Point[] { topLeft, topRight, bottomRight, bottomLeft };
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(points, new byte[] { (byte)System.Drawing.Drawing2D.PathPointType.Start, (byte)System.Drawing.Drawing2D.PathPointType.Line, (byte)System.Drawing.Drawing2D.PathPointType.Line, (byte)System.Drawing.Drawing2D.PathPointType.Line });
            gp.Transform(matrix);
            return System.Drawing.Rectangle.Round(gp.GetBounds());
        }

        private int _sourceTotCount = 0;
        public int SourceTotalCount
        {
            get
            {
                if (ListAugmentTypeClass.Count > 0)
                    _sourceTotCount = ListAugmentTypeClass.Sum(item => Convert.ToInt32(item.ClassCount));

                return _sourceTotCount;
            }

            set
            {
                _sourceTotCount = value;
                NotifyPropertyChanged("SourceTotalCount");
            }
        }

        private int _augmentExportCount = 0;
        public int AugmentExportCount
        {
            get
            {
                if (ListAugmentTypeClass.Count > 0)
                    _augmentExportCount = ListAugmentTypeClass.Sum(item => item.AugmentStatCount);

                return _augmentExportCount;
            }

            set
            {
                _augmentExportCount = value;
                NotifyPropertyChanged("AugmentExportCount");
            }
        }

        private void GetAugmentationforImage(ImageClass curImageClass, BitmapImage bmpImage, string strOutputPath, string ImageName, Shape shapeROI,
            StringBuilder sbCSVdata, string regionCount, AugmentTypeClass curAugmentClass = null, string strAugType = null)
        {
            string seperator = ",";
            string filename = System.IO.Path.GetFileNameWithoutExtension(ImageName);
            string strRegion = "{\"class id\":\"" + curImageClass.ClassIndex + "\", \"class name\":\"" + curImageClass.ClassAlias + "\"}";
            string strShape = (curImageClass.Shape == EnumSelectedShape.Rectangle) ? "rect" : (curImageClass.Shape == EnumSelectedShape.Circle) ? "circle" :
                                    (curImageClass.Shape == EnumSelectedShape.Polyline) ? "polyline" : "";


            //Image Augmentation for Horizontal Flip
            if ((curAugmentClass != null && curAugmentClass.IsHFlipSelected) || (strAugType != null && strAugType.Contains("AH")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Horizontal Flip\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Horizontal Flip\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath, filename + "_AH.bmp");
                Image augmentImage = new Image();
                string ShapeCoordinates = "";
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleX = -1;
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    augmentImage.Source = bmpImage;
                    augmentImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    augmentImage.RenderTransform = flipTrans;

                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);                   
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    AugmentCanvas.RenderTransform = flipTrans;

                    double rendered_X = AugmentCanvas.Width - curImageClass.XCoordinate - curImageClass.Width;
                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);
                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + curImageClass.YCoordinate +
                                        ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }
                else
                {
                    augmentImage.Source = bmpImage;
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    AugmentCanvas.RenderTransform = flipTrans;
                    double rendered_X = AugmentCanvas.Width - curImageClass.XCoordinate - curImageClass.Width;

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    augmentImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    augmentImage.RenderTransform = flipTrans;

                    PngBitmapEncoder renderedOriginalBmp = RenderBitmapImage(augmentImage);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strUnlabelledDataPath + filename + "_AH.bmp"))
                    {
                        renderedOriginalBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);
                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + curImageClass.YCoordinate +
                                        ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }

                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AH.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Horizontal Flip"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AH");
            }

            //Image Augmentation for Vertical Flip
            if ((curAugmentClass != null && curAugmentClass.IsVFlipSelected) || (strAugType != null && strAugType.Contains("AV")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Vertical Flip\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Vertical Flip\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath, filename + "_AV.bmp");
                Image augmentImage = new Image();
                string ShapeCoordinates = "";
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleY = -1;
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    augmentImage.Source = bmpImage;
                    augmentImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    augmentImage.RenderTransform = flipTrans;

                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    AugmentCanvas.RenderTransform = flipTrans;

                    double rendered_Y = AugmentCanvas.Height - curImageClass.YCoordinate - curImageClass.Height;
                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);
                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + curImageClass.XCoordinate + ", \"y\": " + rendered_Y +
                                                ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }
                else
                {
                    augmentImage.Source = bmpImage;
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    AugmentCanvas.RenderTransform = flipTrans;
                    double rendered_Y = AugmentCanvas.Height - curImageClass.YCoordinate - curImageClass.Height;

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    augmentImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    augmentImage.RenderTransform = flipTrans;

                    PngBitmapEncoder renderedOriginalBmp = RenderBitmapImage(augmentImage);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strUnlabelledDataPath + filename + "_AV.bmp"))
                    {
                        renderedOriginalBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);
                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + curImageClass.XCoordinate + ", \"y\": " + rendered_Y +
                                                ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }
                
                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AV.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Vertical Flip"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AV");
            }

            //Image Augmentation for Noise
            if ((curAugmentClass != null && curAugmentClass.IsNoiseSelected) || (strAugType != null && strAugType.Contains("AN")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Noise\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Noise\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath + "\\" + filename + "_AN.bmp");
                Image augmentImage = new Image();
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    augmentImage.Source = bmpImage;
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);
                }
                else
                {
                    SaltAndPepperNoise filter = new SaltAndPepperNoise(settings.CurrentAugmentConfig.NoiseValue);
                    System.Drawing.Bitmap tempBitmap = BitmapImage2Bitmap(bmpImage);
                    System.Drawing.Bitmap noiseBitmap = filter.Apply(tempBitmap);

                    augmentImage = new Image();
                    augmentImage.Source = ImageSourceFromBitmap(noiseBitmap);
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    noiseBitmap.Save(strUnlabelledDataPath + filename + "_AN.bmp");
                    AugmentCanvas.Children.Remove(shapeROI);
                }

                string ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + curImageClass.XCoordinate + ", \"y\": " + curImageClass.YCoordinate +
                                            ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AN.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Noise"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AN");
            }

            //Image Augmentation for Rotation
            if ((curAugmentClass != null && curAugmentClass.IsRotSelected) || (strAugType != null && strAugType.Contains("AR")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Rotate\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Rotate\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath + "\\" + filename + "_AR.bmp");
                string ShapeCoordinates = "";
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    System.Drawing.Bitmap tempBitmap = BitmapImage2Bitmap(bmpImage);
                    System.Drawing.Bitmap reversedBitMap = RotateImage(tempBitmap, -settings.CurrentAugmentConfig.RotateDegree);
                    System.Drawing.Bitmap bmpShape = GetImagewithShape(reversedBitMap, curImageClass);
                    System.Drawing.Bitmap rotatedBitMap = RotateImage(bmpShape, settings.CurrentAugmentConfig.RotateDegree);
                    rotatedBitMap.Save(strLabelledImageName);

                    var angle = settings.CurrentAugmentConfig.RotateDegree * Math.PI / 180.0f;
                    //Find rotate orgin
                    double rotateOrginX = tempBitmap.Width / 2;
                    double rotateOrginY = tempBitmap.Height / 2;

                    var cosA = Math.Cos(angle);
                    var sinA = Math.Sin(angle);

                    var rendered_X = (float)(cosA * (curImageClass.XCoordinate) - sinA * (curImageClass.YCoordinate));
                    var rendered_Y = (float)(sinA * (curImageClass.XCoordinate) + cosA * (curImageClass.YCoordinate));

                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + rendered_Y +
                                ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";

                }
                else
                {
                    System.Drawing.Bitmap tempBitmap = BitmapImage2Bitmap(bmpImage);
                    System.Drawing.Bitmap bmpShape = GetImagewithShape(tempBitmap, curImageClass);
                    System.Drawing.Bitmap rotatedBitMap = RotateImage(bmpShape, settings.CurrentAugmentConfig.RotateDegree);
                    rotatedBitMap.Save(strLabelledImageName);

                    //Rotate without Annotation
                    tempBitmap = BitmapImage2Bitmap(bmpImage);
                    System.Drawing.Bitmap rotateUnlabelledBitMap = RotateImage(tempBitmap, settings.CurrentAugmentConfig.RotateDegree);
                    rotateUnlabelledBitMap.Save(strUnlabelledDataPath + filename + "_AR.bmp");

                    var angle = settings.CurrentAugmentConfig.RotateDegree * Math.PI / 180.0f;
                    //Find rotate orgin
                    double rotateOrginX = tempBitmap.Width / 2;
                    double rotateOrginY = tempBitmap.Height / 2;

                    var cosA = Math.Cos(angle);
                    var sinA = Math.Sin(angle);

                    var rendered_X = (float)(cosA * (curImageClass.XCoordinate) - sinA * (curImageClass.YCoordinate));
                    var rendered_Y = (float)(sinA * (curImageClass.XCoordinate) + cosA * (curImageClass.YCoordinate));

                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + rendered_Y +
                                ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }

                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AR.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Rotate"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AR");
            }

            //Image Augmentation for Translate/Shift
            if ((curAugmentClass != null && curAugmentClass.IsTransSelected) || (strAugType != null && strAugType.Contains("AT")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Trans\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Trans\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath + "\\" + filename + "_AT.bmp");
                Image augmentImage = new Image();
                string ShapeCoordinates = "";
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    augmentImage.Source = bmpImage;
                    TranslateTransform shiftTrans = new TranslateTransform(0, 0);
                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(shiftTrans);
                    shiftTrans.X = -settings.CurrentAugmentConfig.Trans_Coordinate[0];
                    shiftTrans.Y = -settings.CurrentAugmentConfig.Trans_Coordinate[1];
                    augmentImage.RenderTransform = tg;

                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    shiftTrans = new TranslateTransform(0, 0);
                    tg = new TransformGroup();
                    tg.Children.Add(shiftTrans);
                    shiftTrans.X = settings.CurrentAugmentConfig.Trans_Coordinate[0];
                    shiftTrans.Y = settings.CurrentAugmentConfig.Trans_Coordinate[1];
                    AugmentCanvas.RenderTransform = tg;
                    double rendered_X = settings.CurrentAugmentConfig.Trans_Coordinate[0] + curImageClass.XCoordinate;
                    double rendered_Y = settings.CurrentAugmentConfig.Trans_Coordinate[1] + curImageClass.YCoordinate;
                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);

                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + rendered_Y +
                                   ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }
                else
                {
                    augmentImage.Source = bmpImage;
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    TranslateTransform shiftTrans = new TranslateTransform(0, 0);
                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(shiftTrans);
                    shiftTrans.X = settings.CurrentAugmentConfig.Trans_Coordinate[0];
                    shiftTrans.Y = settings.CurrentAugmentConfig.Trans_Coordinate[1];
                    AugmentCanvas.RenderTransform = tg;
                    double rendered_X = settings.CurrentAugmentConfig.Trans_Coordinate[0] + curImageClass.XCoordinate;
                    double rendered_Y = settings.CurrentAugmentConfig.Trans_Coordinate[1] + curImageClass.YCoordinate;
                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    augmentImage.RenderTransform = tg;
                    PngBitmapEncoder renderedOriginalBmp = RenderBitmapImage(augmentImage);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strUnlabelledDataPath + filename + "_AT.bmp"))
                    {
                        renderedOriginalBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    AugmentCanvas.Children.Remove(shapeROI);

                    ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + rendered_X + ", \"y\": " + rendered_Y +
                                   ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                }

                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AT.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Translate"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AT");
            }

            //Image Augmentation for Blur
            if ((curAugmentClass != null && curAugmentClass.IsBlurSelected) || (strAugType != null && strAugType.Contains("AB")))
            {
                string strImageDataPath = System.IO.Path.Combine(strOutputPath, @"Blur\Labelled\" + curImageClass.ClassAlias);
                if (!Directory.Exists(strImageDataPath))
                    Directory.CreateDirectory(strImageDataPath);

                string strUnlabelledDataPath = System.IO.Path.Combine(strOutputPath, @"Blur\Unlabelled\");
                if (!Directory.Exists(strUnlabelledDataPath))
                    Directory.CreateDirectory(strUnlabelledDataPath);

                //Image image = augmentCanvas.Children[0] as Image;
                //int width = Convert.ToInt32(image.Width);
                //int height = Convert.ToInt32(image.Height);
                //System.Drawing.Bitmap blurred = new System.Drawing.Bitmap(width, height);

                //Emgu.CV.Image<Emgu.CV.Structure.Gray, float> inputImage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, float>(blurred);
                //Emgu.CV.Image<Emgu.CV.Structure.Gray, float> smoothedImage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, float>(inputImage.Width, inputImage.Height);

                //Emgu.CV.CvInvoke.Blur(inputImage, smoothedImage, new System.Drawing.Size(5,5), new System.Drawing.Point(0,0));
                //System.Drawing.Bitmap blurredBitmap = smoothedImage.ToBitmap();

                string strLabelledImageName = System.IO.Path.Combine(strImageDataPath + "\\" + filename + "_AB.bmp");
                Image augmentImage = new Image();
                if (File.Exists(strLabelledImageName))
                {
                    bmpImage = new BitmapImage();
                    try
                    {
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strLabelledImageName))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                        }
                    }
                    catch { }

                    augmentImage.Source = bmpImage;
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, augmentImage);
                    AugmentCanvas.Children.Insert(0, augmentImage);

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }
                    AugmentCanvas.Children.Remove(shapeROI);
                }
                else
                {
                    GaussianBlur filter = new GaussianBlur(settings.CurrentAugmentConfig.BlurRatio, 9);
                    System.Drawing.Bitmap tempBitmap = BitmapImage2Bitmap(bmpImage);
                    System.Drawing.Bitmap blurredBitmap = filter.Apply(tempBitmap);

                    Image tempImg = new Image();
                    tempImg.Source = ImageSourceFromBitmap(blurredBitmap);
                    Canvas AugmentCanvas = GetAugmentCanvas(bmpImage, shapeROI, tempImg);
                    AugmentCanvas.Children.Insert(0, tempImg);

                    PngBitmapEncoder renderedBmp = RenderBitmapImage(AugmentCanvas);
                    using (Stream stm = Delimon.Win32.IO.File.Create(strLabelledImageName))
                    {
                        renderedBmp.Save(stm);
                        stm.Flush();
                        stm.Close();
                    }

                    blurredBitmap.Save(strUnlabelledDataPath + filename + "_AB.bmp");

                    AugmentCanvas.Children.Remove(shapeROI);
                }
                
                string ShapeCoordinates = "{\"name\":\"" + strShape + "\", \"x\": " + curImageClass.XCoordinate + ", \"y\": " + curImageClass.YCoordinate +
                                 ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                sbCSVdata.AppendLine(string.Join(seperator, filename + "_AB.bmp", regionCount, "\"" + ShapeCoordinates.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\"", "Blur"));

                if (curAugmentClass == null && strAugType != null)
                    return;
                else
                    UpdateAugmentTypeStat(curAugmentClass, "AB");
            }
        }

        private System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }


        private void ExportAugmentationReportToPDF(string strDataPath)
        {
            string strPDFSavePath = System.IO.Path.Combine(strDataPath, "Report_" + DateTime.Now.ToString("ddMMyyyy_HHmmss"));
            pdfExport.InitSettings(20, 20, 15, 20, strPDFSavePath);
            pdfExport.InitPdf();
            pdfExport.AppendTextHeading("Genie Supervisor", true);
            pdfExport.AppendTextHeading("Augmentation Report", false);
            string[] TopLinedata = new string[2];
            TopLinedata[0] = "Date   : " + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
            TopLinedata[1] = "Project : " + settings.dictProjectList[settings.CurrentProject];
            pdfExport.AppendDateString(TopLinedata);

            TopLinedata = new string[2];
            TopLinedata[0] = "Total Regions : " + SourceTotalCount;
            TopLinedata[1] = "Total Augmentation : " + AugmentExportCount;
            pdfExport.AppendDateString(TopLinedata);
            pdfExport.AppendLine();

            int nAugmentType = 8;
            string[] straHeader = new string[nAugmentType];
            float[] colwidths = new float[nAugmentType];

            straHeader[0] = "Class Name";
            straHeader[1] = EnumAugmentionType.FlipH.ToString();
            straHeader[2] = EnumAugmentionType.FlipV.ToString();
            straHeader[3] = EnumAugmentionType.Noise.ToString();
            straHeader[4] = EnumAugmentionType.Rotate.ToString();
            straHeader[5] = EnumAugmentionType.Trans.ToString();
            straHeader[6] = EnumAugmentionType.Blur.ToString();
            straHeader[7] = "Total Count";

            colwidths[0] = 5f;
            for (int i = 1; i < nAugmentType; i++)
                colwidths[i] = 1f;

            pdfExport.AppendTableHeader(straHeader, colwidths, 0, 0);

            List<string> listTableContent;
            foreach (AugmentTypeClass curAugmentClass in ListAugmentTypeClass)
            {
                listTableContent = new List<string>();
                listTableContent.Add(curAugmentClass.ClassName);
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeHorizontalCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeVerticalCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeNoiseCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeRotateCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeTransCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentTypestats.TypeBlurCount.ToString());
                listTableContent.Add(curAugmentClass.AugmentStatCount.ToString());

                pdfExport.AppendTableRows(listTableContent, 0, 15);
            }

            pdfExport.CloseFile();
        }

        private void SaveLastAugmentedStatHistory(List<string> listAugTypePool = null)
        {
            string Workdir = settings.StatsFilePath + @"\GenieSupervisor_WorkStats";
            if (!Directory.Exists(Workdir))
                Directory.CreateDirectory(Workdir);

            string[] StatsFile = Directory.GetFiles(Workdir, "*AugmentStat*.bin");
            string serializationFile = System.IO.Path.Combine(Workdir, "AugmentStat_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");

            using (MemoryStream stream = new MemoryStream())
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                string LastAugmentDate = dtLastAugmentTime.ToString();
                bformatter.Serialize(stream, LastAugmentDate);
                char augmentMode = radNormal.IsChecked.Value ? 'N' : 'R';
                bformatter.Serialize(stream, augmentMode);

                if(augmentMode == 'R' && listAugTypePool != null)
                {
                    bformatter.Serialize(stream, txtBatchSize.Text.Trim());
                    bformatter.Serialize(stream, listAugTypePool);
                }
                bformatter.Serialize(stream, ListAugmentTypeClass.Count);
                for (int count = 0; count < ListAugmentTypeClass.Count; count++)
                {
                    AugmentTypeClass curAugmentType = ListAugmentTypeClass[count] as AugmentTypeClass;
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.AliasName);
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.ClassName);
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.Count);
                    bformatter.Serialize(stream, curAugmentType.AugmentStatCount);
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
        }

        private void LoadLastAugmentedStatHistory()
        {
            try
            {
                string Workdir = settings.StatsFilePath + @"\GenieSupervisor_WorkStats";
                string[] StatsFile = Directory.GetFiles(Workdir, "*AugmentStat*.bin");
                if (StatsFile.Length == 0)
                    return;

                string deSerializFile = System.IO.Path.Combine(Workdir, StatsFile[0]);
                var converter = new System.Windows.Media.BrushConverter();

                using (Stream stream = File.Open(deSerializFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    string LastAugmentDate = (string)bformatter.Deserialize(stream);
                    dtLastAugmentTime = Convert.ToDateTime(LastAugmentDate);

                    char augmentMode = (char)bformatter.Deserialize(stream);
                    if (augmentMode == 'N')
                        radNormal.IsChecked = true;
                    else
                        radRandom.IsChecked = true;

                    if (augmentMode == 'R')
                    {
                        txtBatchSize.Text = (string)bformatter.Deserialize(stream);
                        var listAugmentType = (List<string>)bformatter.Deserialize(stream);

                        tgAH.IsChecked = listAugmentType.Contains("AH");
                        tgAV.IsChecked = listAugmentType.Contains("AV");
                        tgAN.IsChecked = listAugmentType.Contains("AN");
                        tgAR.IsChecked = listAugmentType.Contains("AR");
                        tgAT.IsChecked = listAugmentType.Contains("AT");
                        tgAB.IsChecked = listAugmentType.Contains("AB");
                    }

                    int nAugmentCount = (int)bformatter.Deserialize(stream);
                    for (int count = 0; count < nAugmentCount; count++)
                    {
                        ClassStats curClassStat = new ClassStats();
                        curClassStat.AliasName = (string)bformatter.Deserialize(stream);
                        curClassStat.ClassName = (string)bformatter.Deserialize(stream);
                        curClassStat.Count = (int)bformatter.Deserialize(stream);

                        AugmentTypeClass curAugmentType = new AugmentTypeClass(curClassStat);
                        curAugmentType.AugmentStatCount = (int)bformatter.Deserialize(stream);

                        ListAugmentTypeClass.Add(curAugmentType);
                    }
                }

                lvStatistics.ItemsSource = ListAugmentTypeClass;
                lvStatistics.Items.Refresh();
                lblAugmentStatus.Content = "Last Augmentation process : " + dtLastAugmentTime.ToShortDateString() + " " + dtLastAugmentTime.ToShortTimeString();
                NotifyPropertyChanged("SourceTotalCount");
                NotifyPropertyChanged("AugmentExportCount");
                Utilities.LogMessage("Saved Augmented Stats Loaded");
            }

            catch (Exception ex)
            {
               Utilities.LogMessage("LoadLastAugmentedStatHistory " + ex.Message, 0);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool DeleteObject([System.Runtime.InteropServices.In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(System.Drawing.Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        
    }

    public class AugmentTypeClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string ClassName
        {
            get
            {
                return AugmentClassStat.ClassName + "(" + AugmentClassStat.AliasName + ")";
            }
        }

        public string ClassAlias
        {
            get
            {
                return AugmentClassStat.AliasName;
            }
        }

        public string ClassCount
        {
            get
            {
                return AugmentClassStat.Count.ToString();
            }
        }

        private int targetcount = 0;
        public int TargetCount
        {
            get
            {
                return targetcount;
            }

            set
            {
                targetcount = value;
                NotifyPropertyChanged("TargetCount");
            }
        }

        private Visibility _columnVisibility = Visibility.Visible;
        public Visibility ColumnVisibility
        {
            get
            {
                return _columnVisibility;
            }

            set
            {
                _columnVisibility = value;
                NotifyPropertyChanged("ColumnVisibility");
            }
        }

        private GridLength _columnWidth = new GridLength(0, GridUnitType.Star);
        public GridLength ColumnWidth
        {
            get
            {
                return _columnWidth;
            }

            set
            {
                _columnWidth = value;
                NotifyPropertyChanged("ColumnWidth");
            }
        }

        public int AugmentExportCount { get; set; }

        public int AugmentStatCount { get; set; }

        public AugmentTypeStat AugmentTypestats { get; set; }

        private ClassStats _augmentClassStat;
        public ClassStats AugmentClassStat
        {
            get
            {
                return _augmentClassStat;
            }
            set
            {
                _augmentClassStat = value;
                NotifyPropertyChanged("ClassCount");
            }
        }

        private bool _IsHFlipSelected = false;
        public bool IsHFlipSelected
        {
            get
            {
                return _IsHFlipSelected;
            }
            set
            {
                _IsHFlipSelected = value;
                NotifyPropertyChanged("IsHFlipSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }

        private bool _IsVFlipSelected = false;
        public bool IsVFlipSelected
        {
            get
            {
                return _IsVFlipSelected;
            }
            set
            {
                _IsVFlipSelected = value;
                NotifyPropertyChanged("IsVFlipSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }

        private bool _IsNoiseSelected = false;
        public bool IsNoiseSelected
        {
            get
            {
                return _IsNoiseSelected;
            }
            set
            {
                _IsNoiseSelected = value;
                NotifyPropertyChanged("IsNoiseSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }
        private bool _IsRotSelected = false;
        public bool IsRotSelected
        {
            get
            {
                return _IsRotSelected;
            }
            set
            {
                _IsRotSelected = value;
                NotifyPropertyChanged("IsRotSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }
        private bool _IsTransSelected = false;
        public bool IsTransSelected
        {
            get
            {
                return _IsTransSelected;
            }
            set
            {
                _IsTransSelected = value;
                NotifyPropertyChanged("IsTransSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }

        private bool _IsBlurSelected = false;
        public bool IsBlurSelected
        {
            get { return _IsBlurSelected; }
            set
            {
                _IsBlurSelected = value;
                NotifyPropertyChanged("IsBlurSelected");
                NotifyPropertyChanged("IsSelectChecked");
            }
        }

        public bool IsTypeEnable
        {
            get
            {
                if (AugmentClassStat.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        private bool _IsSelectChecked = false;
        public bool IsSelectChecked
        {
            get
            {
                if (IsHFlipSelected || IsVFlipSelected || IsNoiseSelected || IsTransSelected || IsRotSelected || IsBlurSelected || _IsSelectChecked)
                    return true;
                else
                    return false;
            }
            set
            {
                _IsSelectChecked = value;
                NotifyPropertyChanged("IsSelectChecked");
            }
        }

        public List<string[]> ListClassAttributes { get; set; }

        public AugmentTypeClass(ClassStats curClassStat)
        {
            AugmentClassStat = curClassStat;
            AugmentTypestats = new AugmentTypeStat();
            TargetCount = curClassStat.Count;
            IsHFlipSelected = false;
            IsVFlipSelected = false;
            IsNoiseSelected = false;
            IsRotSelected = false;
            IsTransSelected = false;
            IsBlurSelected = false;
            ListClassAttributes = new List<string[]>();
        }
    }

    public class AugmentTypeStat
    {
        public int TypeHorizontalCount { get; set; }

        public int TypeVerticalCount { get; set; }

        public int TypeNoiseCount { get; set; }

        public int TypeRotateCount { get; set; }

        public int TypeTransCount { get; set; }

        public int TypeBlurCount { get; set; }

    }
}
