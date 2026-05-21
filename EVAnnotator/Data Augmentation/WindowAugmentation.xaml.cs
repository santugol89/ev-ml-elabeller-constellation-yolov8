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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls;

namespace GenieSupervisor.Data_Augmentation
{
    /// <summary>
    /// Interaction logic for WindowAugmentation.xaml
    /// </summary>
    public partial class WindowAugmentation : Telerik.Windows.Controls.RadWindow, INotifyPropertyChanged
    {
        MainWindow app;
        public event PropertyChangedEventHandler PropertyChanged;

        public WindowAugmentation(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            tbTotalRegionHeader.Text = app.settings.ClassType == EnumClassType.Segregation ? "Total Images" : "Total Regions";
            tbAnnotateRegionHeader.Text = app.settings.ClassType == EnumClassType.Segregation ? "Raw Segregated Images" : "Raw Annotated Regions";
            tbAugmentRegionHeader.Text = app.settings.ClassType == EnumClassType.Segregation ? "Total Augmented Images" : "Total Augmented Regions";
            tbRegionCount.Text = app.settings.ClassType == EnumClassType.Segregation ? "Raw Segregated Images Count" : "Raw Annotated Region Count";
            foreach (AugmentTypeClass curType in app.ListDataAugmentTypeClass)
            {
                curType.ColumnVisibility = app.settings.ClassType == EnumClassType.Segregation ? Visibility.Visible : Visibility.Collapsed;
                curType.ColumnWidth = app.settings.ClassType == EnumClassType.Segregation ? new GridLength(0.12, GridUnitType.Star) : new GridLength(0, GridUnitType.Star);
            }

            RotationColumnWidth = app.settings.ClassType == EnumClassType.Segregation ? new GridLength(0.12, GridUnitType.Star) : new GridLength(0, GridUnitType.Star);
            ListAugmentationView.ItemsSource = app.ListDataAugmentTypeClass;
            lvStatistics.ItemsSource = app.ListDataAugmentTypeClass;
            lblNotify.Visibility = app.ListDataAugmentTypeClass.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
            this.DataContext = this;
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
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

            _windowAugmentationConfig = new AugmentationConfigWindow(app, AugType);
            _windowAugmentationConfig.WindowStartupLocation = WindowStartupLocation.Manual;
            _windowAugmentationConfig.Left = p.X - 400;
            _windowAugmentationConfig.Top = p.Y + 100 < SystemParameters.PrimaryScreenHeight ? p.Y + 20 : p.Y - 100;
            _windowAugmentationConfig.Owner = this;
            // Attach an event handler to the Closed event
            _windowAugmentationConfig.Closed += AugmentationConfigWindow_Closed;
            _windowAugmentationConfig.Show();            
        }

        private void AugmentationConfigWindow_Closed(object sender, WindowClosedEventArgs e)
        {
            NotifyPropertyChanged("RotateAngle");
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private GridLength _rotationColumnWidth = new GridLength(0,GridUnitType.Star);
        public GridLength RotationColumnWidth
        {
            get
            {
                return _rotationColumnWidth;
            }

            set
            {
                _rotationColumnWidth = value;
                NotifyPropertyChanged("RotationColumnWidth");
            }
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get
            {
                return AnnotatedRegionCount + AugmentedTotalCount;
            }

            set
            {
                _totalCount = value;
                NotifyPropertyChanged("TotalCount");
            }
        }

        private int _annotatedRegionCount = 0;
        public int AnnotatedRegionCount
        {
            get
            {
                if (app.ListDataAugmentTypeClass.Count > 0)
                    _annotatedRegionCount = app.ListDataAugmentTypeClass.Sum(item => Convert.ToInt32(item.ClassCount));

                return _annotatedRegionCount;
            }

            set
            {
                _annotatedRegionCount = value;
                NotifyPropertyChanged("AnnotatedRegionCount");
            }
        }

        private int _augmentedTotalCount = 0;
        public int AugmentedTotalCount
        {
            get
            {
                if (app.ListDataAugmentTypeClass.Count > 0)
                    _augmentedTotalCount = app.ListDataAugmentTypeClass.Sum(item => item.AugmentStatCount);

                return _augmentedTotalCount;
            }

            set
            {
                _augmentedTotalCount = value;
                NotifyPropertyChanged("AugmentedTotalCount");
            }
        }

        private string _rotateAngle = "";
        public string RotateAngle
        {
            get
            {
                return "(" + app.settings.CurrentAugmentConfig.RotateDegree.ToString() + ")";
            }

            set
            {
                NotifyPropertyChanged("RotateAngle");
            }
        }

        private void chkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (AugmentTypeClass curClass in app.ListDataAugmentTypeClass)
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
            curClass.IsNoiseSelected = false; // (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsRotSelected = app.settings.ClassType == EnumClassType.Segregation? (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false : false;
            curClass.IsTransSelected = false; // (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
            curClass.IsBlurSelected = false; // (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? true : false;
        }

        private void txtTarget_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if ((sender as System.Windows.Controls.TextBox).Text == "0" || (sender as System.Windows.Controls.TextBox).SelectedText == (sender as System.Windows.Controls.TextBox).Text)
                (sender as System.Windows.Controls.TextBox).Text = "";

            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
            AugmentTypeClass curClass = (sender as System.Windows.Controls.TextBox).DataContext as AugmentTypeClass;
            if (Convert.ToInt32((sender as System.Windows.Controls.TextBox).Text + e.Text) > Convert.ToInt32(curClass.ClassCount))
                e.Handled = true;
        }

        private void txtTarget_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            AugmentTypeClass curClass = (sender as System.Windows.Controls.TextBox).DataContext as AugmentTypeClass;
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if ((sender as System.Windows.Controls.TextBox).Text == "")
                    (sender as System.Windows.Controls.TextBox).Text = "0";
            }

            else if ((e.Key == Key.Enter || e.Key == Key.Down))
            {
                int index = app.ListDataAugmentTypeClass.IndexOf(curClass);
                if (index < ListAugmentationView.Items.Count)
                {
                    ListAugmentationView.SelectedIndex = index + 1;
                    AugmentTypeClass nextClassStat = app.ListDataAugmentTypeClass[ListAugmentationView.SelectedIndex];
                    if (nextClassStat.IsTypeEnable)
                        SetTextBoxFocus(nextClassStat);
                }
            }
            else if (e.Key == Key.Up)
            {
                int index = app.ListDataAugmentTypeClass.IndexOf(curClass);
                if (index > 0)
                {
                    ListAugmentationView.SelectedIndex = index - 1;
                    AugmentTypeClass prevClassStat = app.ListDataAugmentTypeClass[ListAugmentationView.SelectedIndex];
                    if (prevClassStat.IsTypeEnable)
                        SetTextBoxFocus(prevClassStat);
                }
            }
            curClass.TargetCount = (sender as System.Windows.Controls.TextBox).Text != "" ? Convert.ToInt32((sender as System.Windows.Controls.TextBox).Text) : 0;
        }

        private void SetTextBoxFocus(AugmentTypeClass setClassStat)
        {
            ItemContainerGenerator generator = ListAugmentationView.ItemContainerGenerator;
            ListBoxItem selectedItem = (ListBoxItem)generator.ContainerFromItem(setClassStat);

            System.Windows.Controls.TextBox tbModifiedID = GetDescendantByType(selectedItem, typeof(System.Windows.Controls.TextBox), "txtTarget") as System.Windows.Controls.TextBox;
            if (tbModifiedID != null)
                tbModifiedID.Focus();
        }

        public Visual GetDescendantByType(Visual element, Type type, string name)
        {
            if (element == null) return null;
            if (element.GetType() == type)
            {
                FrameworkElement fe = element as FrameworkElement;
                if (fe != null)
                {
                    if (fe.Name == name)
                    {
                        return fe;
                    }
                }
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();
            for (int i = 0;
                i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type, name);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        private void txtBatchSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void btnAugment_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (app.ListDataAugmentTypeClass.Count == 0)
            {
                MessageBox.Show("Please split data into train/val/test first before proceeding to augmentation..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None);
                Utilities.LogMessage("Please split train/val/test before proceeding augmentation.", 0);
                return;
            }

            app.ListDataAugmentTypeClass.ForEach(item => item.AugmentExportCount = 0);
            app.ListDataAugmentTypeClass.ForEach(item => item.AugmentStatCount = 0);
            app.ListDataAugmentTypeClass.ForEach(item => item.AugmentTypestats = new AugmentTypeStat());
            List<string> listAugmentTypePool = GetAugmentTypePool();

            if (radNormal.IsChecked.Value && app.ListDataAugmentTypeClass.Where(item => item.IsSelectChecked).Count() == 0)
            {
                System.Windows.MessageBox.Show("No Augmentation types has been selected to proceed..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (radRandom.IsChecked.Value)
            {
                if (listAugmentTypePool.Count == 0)
                {
                    System.Windows.MessageBox.Show("Please select Augmentation type..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (string.IsNullOrEmpty(txtBatchSize.Text.Trim()) || Convert.ToInt32(txtBatchSize.Text.Trim()) == 0)
                {
                    System.Windows.MessageBox.Show("Batch size cannot be blank/zero..!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (Convert.ToInt32(txtBatchSize.Text.Trim()) > AnnotatedRegionCount)
                {
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

            app.bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            busyIndicator.IsBusy = true;
            app.bgWorker.DoWork += bgwDowork_ImageAugmentationProcess;
            app.bgWorker.RunWorkerAsync(arrArgs);
        }

        private void bgwDowork_ImageAugmentationProcess(object sender, DoWorkEventArgs e)
        {
            if (app.bgWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] arrArgs = e.Argument as object[];
                Thread threadSplit = new Thread(() => ImageAugmentationProcess(arrArgs));
                threadSplit.IsBackground = true;
                threadSplit.Start();
            }
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

        public int BusyProgress
        {
            get => _busyProgress;
            set { _busyProgress = value; NotifyPropertyChanged("BusyProgress"); }
        }
        private int _busyProgress;

        public string BusyMessage
        {
            get => _busyMessage;
            set { _busyMessage = value; NotifyPropertyChanged("BusyMessage"); }
        }
        private string _busyMessage;

        public async void ImageAugmentationProcess(object[] arrArgs)
        {
            try
            {
                app.labelEvent.Reset();
                app.SaveEvent.Reset();
                char Type = (char)arrArgs[0];
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
                string strTrainDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + app.settings.Architecture + @"\" + app.settings.trainFolder;

                if (app.settings.ClassType == EnumClassType.Segregation)
                {
                    //Delete already exists augmented images
                    DeleteExistsAugmentedImages(strTrainDataSetPath);

                    int globalIndex = 0;   // counts all processed images
                    int grandTotal = 0;
                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        grandTotal += curAugmentClass.ListClassAttributes.Count;
                        if (curAugmentClass.IsHFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if (curAugmentClass.IsVFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if (curAugmentClass.IsRotSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;
                    }
                    Dispatcher.Invoke(() => busyIndicator.ProgressValue = 0);

                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        for (int i = 0; i < curAugmentClass.ListClassAttributes.Count; i++)
                        {
                            globalIndex++;  // increment global count
                            double overallPct = (globalIndex / (double)grandTotal) * 100.0;
                            double classPct = ((i + 1) / (double)curAugmentClass.ListClassAttributes.Count) * 100.0;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                BusyProgress = (int)classPct;
                                BusyMessage = $"Processing {curAugmentClass.AugmentClassStat.ClassName} : {BusyProgress}%";
                                busyIndicator.ProgressValue = (int)overallPct;
                            });

                            string strImageName = curAugmentClass.ListClassAttributes[i].First();
                            if (string.IsNullOrEmpty(strImageName))
                                continue;
                            string strImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strImageName);
                            if (!File.Exists(strImagePath))
                                continue;

                            var info = HelperImagingclass.GetOriginalImageFormat(strImagePath);
                            PixelFormat wpfPixelFormat = HelperImagingclass.ConvertToWpfPixelFormat(info.pixelFormat);
                            if (curAugmentClass.IsHFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }

                                    Image augmentImage = new Image();
                                    flipTrans.ScaleX = -1;                                   
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AH.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();

                                }, System.Windows.Threading.DispatcherPriority.Background);

                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Horizontally augmented.", 5);
                            }

                            if (curAugmentClass.IsVFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    flipTrans.ScaleY = -1;
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AV.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();

                                }, System.Windows.Threading.DispatcherPriority.Background);
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Vertically augmented.", 5);
                            }

                            if (curAugmentClass.IsRotSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    RotateTransform rotateTransform = new RotateTransform(-app.settings.CurrentAugmentConfig.RotateDegree);
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = rotateTransform;

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AR" + app.settings.CurrentAugmentConfig.RotateDegree.ToString() + ".bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();
                                }, System.Windows.Threading.DispatcherPriority.Background);
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image rotated with " + app.settings.CurrentAugmentConfig.RotateDegree + " degree.", 5);
                            }
                        }
                    }
                }
                else if (app.settings.ClassType == EnumClassType.Rectangle)
                {
                    string strImage = app.settings.Architecture == app.settings.DetectionAlias || app.settings.Architecture == app.settings.SegmentationAlias ? "images" : "Images";
                    string strTrainImagesPath = System.IO.Path.Combine(strTrainDataSetPath, strImage);
                    if(!Directory.Exists(strTrainImagesPath) || Directory.GetFiles(strTrainImagesPath).Length == 0)
                    {
                        app.labelEvent.Set();
                        app.SaveEvent.Set();
                        OnWorkerMethodComplete("Complete");
                        Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show("No Images found in train folder.\nPlease Do train/val/test Datasplit operation before proceeding to augmentation.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    //Delete already exists augmented images
                    DeleteExistsAugmentedImages(strTrainImagesPath);

                    string seperator = ",";
                    StringBuilder sbCSVdata = new StringBuilder();
                    sbCSVdata.AppendLine("filename,region_count,region_shape_attributes,region_attributes");

                    int globalIndex = 0;   // counts all processed images
                    int grandTotal = 0;
                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        grandTotal += curAugmentClass.ListClassAttributes.Count;
                        if(curAugmentClass.IsHFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if(curAugmentClass.IsVFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if(curAugmentClass.IsRotSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;
                    }
                    Dispatcher.Invoke(() => busyIndicator.ProgressValue = 0);                    
                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        for (int i = 0; i < curAugmentClass.ListClassAttributes.Count; i++)
                        {
                            globalIndex++;  // increment global count

                            double overallPct = (globalIndex / (double)grandTotal) * 100.0;
                            double classPct = ((i + 1) / (double)curAugmentClass.ListClassAttributes.Count) * 100.0;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                BusyProgress = (int)classPct;
                                BusyMessage = $"Processing {curAugmentClass.AugmentClassStat.ClassName} : {BusyProgress}%";
                                busyIndicator.ProgressValue = (int)overallPct;
                            });

                            string strImageName = curAugmentClass.ListClassAttributes[i].First();
                            Utilities.LogMessage(curAugmentClass.ListClassAttributes.Count.ToString() + " , " + i.ToString(), 0);
                            if (string.IsNullOrEmpty(strImageName))
                                continue;
                            string strImagePath = System.IO.Path.Combine(strTrainImagesPath, strImageName);
                            if (!File.Exists(strImagePath))
                                continue;

                            string[] arrShapeAttribute = Regex.Split(curAugmentClass.ListClassAttributes[i][2], @",(?=[^\]]*(?:\[|$))");
                            if (arrShapeAttribute.Length < 5)
                                continue;
                            double X = 0, Y = 0, Width = 0, Height = 0;

                            string strTempShape = arrShapeAttribute[0].Substring(arrShapeAttribute[0].LastIndexOf(':') + 1).ToLower(); 
                            X = Convert.ToDouble(arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1));
                            Y = Convert.ToDouble(arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1));
                            Width = Convert.ToDouble(arrShapeAttribute[3].Substring(arrShapeAttribute[3].LastIndexOf(':') + 1));
                            Height = Convert.ToDouble(arrShapeAttribute[4].Substring(arrShapeAttribute[4].LastIndexOf(':') + 1));

                            string shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"x\":" + X + ", \"y\": " + Y +
                                                    ", \"width\": " + Width + ", \"height\": " + Height + " }";
                            string regionCount = curAugmentClass.ListClassAttributes[i][1].Trim().ToString();
                            string strRegion = "{\"class id\":\"" + curAugmentClass.AugmentClassStat.ClassID + "\", \"class name\":\"" + curAugmentClass.AugmentClassStat.AliasName + "\"}";
                            sbCSVdata.AppendLine(string.Join(seperator, strImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));

                            var info = HelperImagingclass.GetOriginalImageFormat(strImagePath);
                            PixelFormat wpfPixelFormat = HelperImagingclass.ConvertToWpfPixelFormat(info.pixelFormat);
                            if (curAugmentClass.IsHFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }

                                    Image augmentImage = new Image();
                                    flipTrans.ScaleX = -1;
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    double rendered_X = Math.Round(AugmentCanvas.Width - X - Width, 3);
                                    //BmpBitmapEncoder renderedBmp = app.RenderBmpBitmapImage(AugmentCanvas);
                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AH.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainImagesPath, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();
                                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"x\":" + rendered_X + ", \"y\": " + Y +
                                                ", \"width\": " + Width + ", \"height\": " + Height + " }";
                                    sbCSVdata.AppendLine(string.Join(seperator, strAugmentImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));
                                }, System.Windows.Threading.DispatcherPriority.Background);

                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Horizontally augmented.", 5);
                            }

                            if (curAugmentClass.IsVFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    flipTrans.ScaleY = -1;
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    double rendered_Y = Math.Round(AugmentCanvas.Height - Y - Height, 3);
                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AV.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainImagesPath, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();

                                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"x\":" + X + ", \"y\": " + rendered_Y +
                                                ", \"width\": " + Width + ", \"height\": " + Height + " }";
                                    sbCSVdata.AppendLine(string.Join(seperator, strAugmentImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));
                                }, System.Windows.Threading.DispatcherPriority.Background);
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Vertically augmented.", 5);
                            }

                            if (curAugmentClass.IsRotSelected)
                            {
                                globalIndex++;
                                Dispatcher.Invoke(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    RotateTransform rotateTransform = new RotateTransform(-app.settings.CurrentAugmentConfig.RotateDegree);
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = rotateTransform;

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AR" + app.settings.CurrentAugmentConfig.RotateDegree.ToString() + ".bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();
                                });
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image rotated with " + app.settings.CurrentAugmentConfig.RotateDegree + " degree.", 5);
                            }
                        }
                    }

                    string strCSVSavePath = System.IO.Path.Combine(strTrainDataSetPath, "train.csv");
                    if (File.Exists(strCSVSavePath))
                    {
                        File.SetAttributes(strCSVSavePath, System.IO.FileAttributes.Normal);
                        File.Delete(strCSVSavePath);
                    }
                    File.WriteAllText(strCSVSavePath, sbCSVdata.ToString());
                    Utilities.LogMessage("train.csv File generated in " + strTrainDataSetPath + " path", 0);
                }
                else if (app.settings.ClassType == EnumClassType.Polyline)
                {
                    string strImage = app.settings.Architecture == app.settings.DetectionAlias || app.settings.Architecture == app.settings.SegmentationAlias ? "images" : "Images";
                    string strTrainImagesPath = System.IO.Path.Combine(strTrainDataSetPath, strImage);
                    if (!Directory.Exists(strTrainImagesPath) || Directory.GetFiles(strTrainImagesPath).Length == 0)
                    {
                        app.labelEvent.Set();
                        app.SaveEvent.Set();
                        OnWorkerMethodComplete("Complete");
                        Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show("No Images found in train folder.\nPlease Do train/val/test Datasplit operation before proceeding to augmentation.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    //Delete already exists augmented images
                    DeleteExistsAugmentedImages(strTrainImagesPath);

                    string seperator = ",";
                    StringBuilder sbCSVdata = new StringBuilder();
                    sbCSVdata.AppendLine("filename,region_count,region_shape_attributes,region_attributes");

                    int globalIndex = 0;   // counts all processed images
                    int grandTotal = 0;
                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        grandTotal += curAugmentClass.ListClassAttributes.Count;
                        if (curAugmentClass.IsHFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if (curAugmentClass.IsVFlipSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;

                        if (curAugmentClass.IsRotSelected)
                            grandTotal += curAugmentClass.ListClassAttributes.Count;
                    }
                    Dispatcher.Invoke(() => busyIndicator.ProgressValue = 0);
                    foreach (AugmentTypeClass curAugmentClass in app.ListDataAugmentTypeClass)
                    {
                        for (int i = 0; i < curAugmentClass.ListClassAttributes.Count; i++)
                        {
                            globalIndex++;  // increment global count

                            double overallPct = (globalIndex / (double)grandTotal) * 100.0;
                            double classPct = ((i + 1) / (double)curAugmentClass.ListClassAttributes.Count) * 100.0;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                BusyProgress = (int)classPct;
                                BusyMessage = $"Processing {curAugmentClass.AugmentClassStat.ClassName} : {BusyProgress}%";
                                busyIndicator.ProgressValue = (int)overallPct;
                            });

                            string strImageName = curAugmentClass.ListClassAttributes[i].First();
                            Utilities.LogMessage(curAugmentClass.ListClassAttributes.Count.ToString() + " , " + i.ToString(), 0);
                            if (string.IsNullOrEmpty(strImageName))
                                continue;
                            string strImagePath = System.IO.Path.Combine(strTrainImagesPath, strImageName);
                            if (!File.Exists(strImagePath))
                                continue;

                            string[] arrShapeAttribute = Regex.Split(curAugmentClass.ListClassAttributes[i][2], @",(?=[^\]]*(?:\[|$))");
                            if (arrShapeAttribute.Length < 3)
                                continue;

                            List<double> all_point_x = new List<double>();
                            List<double> all_point_y = new List<double>();
                            string subString_x = arrShapeAttribute[1].Substring(arrShapeAttribute[1].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                            all_point_x = subString_x.Split(',').Select(double.Parse).ToList();

                            string subString_y = arrShapeAttribute[2].Substring(arrShapeAttribute[2].LastIndexOf(':') + 1).Replace("[", "").Replace("]", "");
                            all_point_y = subString_y.Split(',').Select(double.Parse).ToList();

                            string strTempShape = arrShapeAttribute[0].Substring(arrShapeAttribute[0].LastIndexOf(':') + 1).ToLower();
                            string shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"all_points_x\": [" +
                                                String.Join(", ", all_point_x) + "], \"all_points_y\": [" + String.Join(", ", all_point_y) + "] }";
                            
                            string regionCount = curAugmentClass.ListClassAttributes[i][1].Trim().ToString();
                            string strRegion = "{\"class id\":\"" + curAugmentClass.AugmentClassStat.ClassID + "\", \"class name\":\"" + curAugmentClass.AugmentClassStat.AliasName + "\"}";
                            sbCSVdata.AppendLine(string.Join(seperator, strImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));

                            var info = HelperImagingclass.GetOriginalImageFormat(strImagePath);
                            PixelFormat wpfPixelFormat = HelperImagingclass.ConvertToWpfPixelFormat(info.pixelFormat);
                            if (curAugmentClass.IsHFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }

                                    Image augmentImage = new Image();
                                    flipTrans.ScaleX = -1;
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    List<double> flipped_x = new List<double>();
                                    for (int a = 0; a < all_point_x.Count; a++)
                                    {
                                        double new_x = Math.Round(AugmentCanvas.Width - all_point_x[a], 3);
                                        flipped_x.Add(new_x);
                                    }

                                    //BmpBitmapEncoder renderedBmp = app.RenderBmpBitmapImage(AugmentCanvas);
                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AH.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainImagesPath, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();

                                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"all_points_x\": [" +
                                                        String.Join(", ", flipped_x) + "], \"all_points_y\": [" + String.Join(", ", all_point_y) + "] }";

                                    sbCSVdata.AppendLine(string.Join(seperator, strAugmentImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));
                                }, System.Windows.Threading.DispatcherPriority.Background);

                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Horizontally augmented.", 5);
                            }

                            if (curAugmentClass.IsVFlipSelected)
                            {
                                globalIndex++;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    flipTrans.ScaleY = -1;
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = flipTrans;

                                    List<double> flipped_y = new List<double>();
                                    for (int a = 0; a < all_point_y.Count; a++)
                                    {
                                        double new_x = Math.Round(AugmentCanvas.Height - all_point_y[a], 3);
                                        flipped_y.Add(new_x);
                                    }

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AV.bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainImagesPath, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();
                                    
                                    shapeCoord = "{\"name\":\"" + strTempShape.Substring(strTempShape.LastIndexOf(':') + 1) + "\", \"all_points_x\": [" +
                                                        String.Join(", ", all_point_x) + "], \"all_points_y\": [" + String.Join(", ", flipped_y) + "] }";
                                    sbCSVdata.AppendLine(string.Join(seperator, strAugmentImageName, regionCount, "\"" + shapeCoord.Replace("\"", "\"\"") + "\"", "\"" + strRegion.Replace("\"", "\"\"") + "\""));
                                }, System.Windows.Threading.DispatcherPriority.Background);
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image Vertically augmented.", 5);
                            }

                            if (curAugmentClass.IsRotSelected)
                            {
                                globalIndex++;
                                Dispatcher.Invoke(() =>
                                {
                                    BitmapImage bmpImage = new BitmapImage();
                                    ScaleTransform flipTrans = new ScaleTransform();
                                    try
                                    {
                                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(strImagePath))
                                        {
                                            bmpImage.BeginInit();
                                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bmpImage.StreamSource = stream;
                                            bmpImage.EndInit();
                                        }
                                    }
                                    catch { }
                                    Image augmentImage = new Image();
                                    RotateTransform rotateTransform = new RotateTransform(-app.settings.CurrentAugmentConfig.RotateDegree);
                                    augmentImage.Source = bmpImage;
                                    Canvas AugmentCanvas = app.GetAugmentCanvasForImage(bmpImage, augmentImage);
                                    AugmentCanvas.Children.Insert(0, augmentImage);

                                    AugmentCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                                    AugmentCanvas.RenderTransform = rotateTransform;

                                    BmpBitmapEncoder renderedBmp = HelperImagingclass.RenderBmpBitmapImage(AugmentCanvas, wpfPixelFormat);
                                    string strAugmentImageName = System.IO.Path.GetFileNameWithoutExtension(strImageName) + "_AR" + app.settings.CurrentAugmentConfig.RotateDegree.ToString() + ".bmp";
                                    string strAugmentedImagePath = System.IO.Path.Combine(strTrainDataSetPath, curAugmentClass.AugmentClassStat.ClassName, strAugmentImageName);
                                    using (Stream stm = Delimon.Win32.IO.File.Create(strAugmentedImagePath))
                                    {
                                        renderedBmp.Save(stm);
                                        stm.Flush();
                                        stm.Close();
                                    }

                                    bmpImage = null;
                                    augmentImage = null;
                                    AugmentCanvas.Children.Clear();
                                    AugmentCanvas = null;
                                    renderedBmp = null;
                                    GC.Collect();
                                });
                                curAugmentClass.AugmentStatCount++;
                                Utilities.LogMessage(strImageName + " image rotated with " + app.settings.CurrentAugmentConfig.RotateDegree + " degree.", 5);
                            }
                        }
                    }

                    string strCSVSavePath = System.IO.Path.Combine(strTrainDataSetPath, "train.csv");
                    if (File.Exists(strCSVSavePath))
                    {
                        File.SetAttributes(strCSVSavePath, System.IO.FileAttributes.Normal);
                        File.Delete(strCSVSavePath);
                    }
                    File.WriteAllText(strCSVSavePath, sbCSVdata.ToString());
                    Utilities.LogMessage("train.csv File generated in " + strTrainDataSetPath + " path", 0);
                }
                app.labelEvent.Set();
                app.SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                NotifyPropertyChanged("AnnotatedRegionCount"); 
                NotifyPropertyChanged("AugmentedTotalCount");
                NotifyPropertyChanged("TotalCount");

                app.SaveAugmentationStatHistory();
                Dispatcher.Invoke(() =>
                {
                    lvStatistics.ItemsSource = app.ListDataAugmentTypeClass;
                    lvStatistics.Items.Refresh();
                    MessageBox.Show("Augmentation process completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information,
                        MessageBoxResult.None);
                    Utilities.LogMessage("Augmentation process completed successfully", 0);
                });
                
            }
            catch (Exception ex)
            {
                app.labelEvent.Set();
                app.SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("Something went wrong..!\n" + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error,
                                            MessageBoxResult.None);
                });
                
                Utilities.LogMessage("MainWindow::ImageAugmentationProcessandExport: " + ex.Message, 9);
            }
        }

        private void DeleteExistsAugmentedImages(string strTrainImagesPath)
        {
            string[] listAugmentedImages = null;
            if (app.settings.ClassType == EnumClassType.Segregation)
                listAugmentedImages = Directory.GetFiles(strTrainImagesPath, "*.*", SearchOption.AllDirectories).Where(file => file.Contains("_AH") || file.Contains("_AV") || file.Contains("_AR")).ToArray();
            else
                listAugmentedImages = Directory.GetFiles(strTrainImagesPath, "*.*", SearchOption.TopDirectoryOnly).Where(file => file.Contains("_AH") || file.Contains("_AV")).ToArray();

            if (listAugmentedImages.Length == 0)
                return;

            foreach(string filePath in listAugmentedImages)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
            Utilities.LogMessage("Existing augmented images deleted.", 0);
        }

        private void OnWorkerMethodComplete(string v)
        {
            Dispatcher.Invoke(() =>
            {
                busyIndicator.IsBusy = false;
            });
        }

        private void RadWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //To Set fullscreen above the taskbar
            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            var pixelWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            var pixelHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            var pixelToDPI = 96.0 / graphics.DpiX;
            this.Width = (pixelWidth * pixelToDPI) - 5;
            this.Height = (pixelHeight * pixelToDPI) - 120;
            this.Left = 5;
            this.Top = 90;
            this.WindowState = WindowState.Normal;
        }
    }
}
