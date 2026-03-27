using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Data;
using System.Globalization;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;
using GenieSupervisor.Data_Augmentation;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Sandeep
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ConfigFilePath;
        public Settings settings;
        public Point StartPoint;
        public Point LastPoint;
        public bool IsFirstPoint = false;
        public bool bIsClassAdded = false;
        public bool bResizing = false;
        public char flagClassOp;
        public EnumSelectedShape SelectedShape = EnumSelectedShape.Null;
        public bool bIsSaved = true;
        public UndoRedoClass<UndoRedoItem> undoRedo;
        public ProgressBarWindow progressBar;
        public bool isClosing = false;
        static Mutex mutex;
        public string DefaultPath = "";
        public string DefaultSize = "";
        public bool bIsOverrideValid = false;
        public bool bIsAppendReview = true;
        public bool bIsListClassSelected = false;
        InterfacePDF pdfExport;
        public bool bWorkCellMode = false;
        public Shape selBoundBox = null;
        public bool bIsLoginSuccess = false;
        public string UserName = "";

        public MainWindow()
        {
            bool createdNew;
            mutex = new Mutex(true, AppDomain.CurrentDomain.FriendlyName, out createdNew);
            if (!createdNew)
            {
                System.Windows.Forms.MessageBox.Show("E-Labeller is already running");
                System.Windows.Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            Utilities.LogMessage("-----------------------------");
            Utilities.LogMessage("   " + DateTime.Now);
            Utilities.LogMessage("Application opening...");

            #region Login User
            //WindowUserLogin userLogin = new WindowUserLogin(this);
            //userLogin.ShowDialog();

            //if (!bIsLoginSuccess)
            //{
            //    Utilities.LogMessage("Login Unsuccessful.. Application Closing...");
            //    System.Windows.Application.Current.Shutdown();
            //    return;
            //} 
            #endregion

            LoadRegistrySettings();
            settings = new Settings(this);
            if (!settings.bIsLoaded)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }

            InitializeClass();
            InitializeControls();
            LoadSavedStatistics();
            LoadLabelledWorkStats();
            DataContext = this;

            //To Set fullscreen above the taskbar
            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            var pixelWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            var pixelHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            var pixelToDPI = 96.0 / graphics.DpiX;
            this.Width = pixelWidth * pixelToDPI;
            this.Height = pixelHeight * pixelToDPI;
            this.Left = 0;
            this.Top = 0;
            this.WindowState = WindowState.Normal;
            //----------
            NotifyPropertyChanged("LoggedAsLabel");
            UpdateNotifyPropertyChnages();
            Utilities.LogMessage("Application Initialized Successfully...");
        }

        public void UpdateNotifyPropertyChnages()
        {
            NotifyPropertyChanged("SelectedProject");
            NotifyPropertyChanged("MultiClassRowHeight");
            NotifyPropertyChanged("ClassNameListViewWidth");
            NotifyPropertyChanged("AttributeListViewWidth");
            NotifyPropertyChanged("LabelStatHeader");
            NotifyPropertyChanged("UnLabelledImagesContent");
            NotifyPropertyChanged("LabelledImagesContent");

            InitializeDataInsightView();
        }

        private void LoadRegistrySettings()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EV-MLEngine");

            ConfigFilePath = (string)key.GetValue("Config Path", "");
            if (String.IsNullOrEmpty(ConfigFilePath))
            {
                ConfigFilePath = @"C:\EV-MLEngine Config\";
                key.SetValue("Config Path", ConfigFilePath);
            }

            if (!Directory.Exists(ConfigFilePath))
            {
                ConfigFilePath = @"C:\EV-MLEngine Config\";
                key.SetValue("Config Path", ConfigFilePath);
            }

            Directory.CreateDirectory(ConfigFilePath);
            if (ConfigFilePath.Substring(ConfigFilePath.Length - 1) != "\\")
                ConfigFilePath += "\\";
            Utilities.LogMessage("Registry Settings loaded");
        }

        /// <summary>
        /// Initialize the classes
        /// </summary>
        private void InitializeClass()
        {            
            undoRedo = new UndoRedoClass<UndoRedoItem>();
            pdfExport = new InterfacePDF();
            Utilities.LogMessage("Classes Initiated");
        }

        /// <summary>
        /// Initialize the Controls eg, combo box, context menu, list box etc
        /// </summary>
        private void InitializeControls()
        {
            UndoButton.IsEnabled = false;
            RedoButton.IsEnabled = false;
            lblZoomStatus.Visibility = Visibility.Collapsed;
            listBoxImages.ItemsSource = ImageMenuList;
            InitializeComboBox();
            drawingSurface.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(drawingSurface_ContextMenuOpening), true);
            drawingSurface.MouseLeave += drawingSurface_MouseLeave;
            menuAdd.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(AddImageAttributeToClass_Click), true);
            menuEdit.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(EditImageAttribute_Click), true);
            menuCopy.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(CopyImageAttribute_Click), true);
            menuPaste.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(PasteImageAttribute_Click), true);
            menuSave.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(SaveROItoDisk_Click), true);
            menuShowBoundBox.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(ShowBoundingBox_Click), true);

            SetApplicationMenuControls();

            Utilities.LogMessage("Controls Initiated");
        }

        public void InitializeComboBox()
        {
            cmbClassName.Items.Clear();
            cmbClassFilter.ItemsSource = null;
            if (ListModifiedClass.Count > 0){
                List<string> listClassAlias = ListModifiedClass.Select(s => s.ModifiedClassName).ToList();
                listClassAlias.Insert(0, "All");
                cmbClassFilter.ItemsSource = new BindingSource(listClassAlias, null);

                string[] arrayValue = settings.dictEVSupervisorClass.Values.ToArray();
                for (int i = 0; i < arrayValue.Length; i++)
                    cmbClassName.Items.Add(arrayValue[i].ToString());

                lblClassLoadStatus.Visibility = Visibility.Collapsed;
            }
            else{
                if (settings.CurrentProject == "P0"){
                    lblClassLoadStatus.Content = "No Project selected.. Please Go to File->Settings to load";
                    lblClassLoadStatus.ToolTip = "Select Project from File->Settings menu";
                }
                else{
                    lblClassLoadStatus.Content = "Class File not found";
                    lblClassLoadStatus.ToolTip = "Select Class from File->Settings menu";
                }
                lblClassLoadStatus.Visibility = Visibility.Visible;
            }
            IsVisibleShapeQuickPallete = settings.CurrentProject == "P0" || bWorkCellMode? Visibility.Collapsed : Visibility.Visible;

            cmbSort.ItemsSource = null;
            List<string> listSortItems = new List<string>();
            if(settings.ClassType == EnumClassType.Segregation)
                listSortItems = new List<string>() { "All Images", "Unsegregated Images", "Segregated Images", "Require Correction" };
            else
                listSortItems = new List<string>() { "All Images", "Unlabelled Images", "Labelled Images", "Require Correction" };

            cmbSort.ItemsSource = listSortItems;
            lblDataInsightHeading.Content = settings.ClassType == EnumClassType.Segregation ? "Classwise Graph - Segregation Image Count" : "Classwise Graph - Image Count";
        }

        /// <summary>
        /// Initialize the Saved Augmentation and AutoPilot Stats etc
        /// </summary>
        private void LoadSavedStatistics()
        {
            //LoadLastAugmentedStatHistory();
            LoadAugmentationStatHistory();
            LoadAutoLabelledStatHistory();
        }

        private void MinimizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Please Confirm to close E-Labeller Application?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Environment.Exit(0);
            base.OnClosed(e);
        }

        /// <summary>
        /// Save the setting while Application closing initiates
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            isClosing = true;
            if(settings != null && settings.bIsLoaded)
            {
                settings.WriteConfigSettings();
                SetFileAttributeNormal();
                ClearStatTempFiles();
                SaveLabelledWorkintoDisk();
            }                
            Utilities.LogMessage("Application Closing...");
            base.OnClosing(e);
        }

        /// <summary>
        /// Performs opertaion when Shape button selected eg. rectangle, circle, polygon
        /// </summary>
        public void ShapeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            object QuickPaletteShape = (sender as System.Windows.Controls.Button).Name == "SelectionRectangle" ? btnQuickRect :
                                        (sender as System.Windows.Controls.Button).Name == "SelectionCircle" ? btnQuickCircle :
                                        (sender as System.Windows.Controls.Button).Name == "SelectionPoly" ? btnQuickPoly :
                                        (sender as System.Windows.Controls.Button).Name == "SelectArrow" ? btnQuickSelect : null;


            SelectedShape = (sender as System.Windows.Controls.Button).Name == "SelectionRectangle" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Rectangle :
                            (sender as System.Windows.Controls.Button).Name == "SelectionCircle" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Circle :
                            (sender as System.Windows.Controls.Button).Name == "SelectionPoly" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Polyline : EnumSelectedShape.Null;

            SetShapeVisibilityWhichSwitch();            //Set shape visible or not while changing shape tool
            SetShapeButtonFocus(QuickPaletteShape, sender);

            if (selShape != null && (!bIsClassAdded || !IsPolyFirstPoint))
            {
                drawingSurface.Children.Remove(selShape);                    //removes previously drawn shape
                if (selLineShape != null)
                    drawingSurface.Children.Remove(selLineShape);
                IsEnableClassStackPanel = false;
                IsPolyFirstPoint = true;
            }
        }

        /// <summary>
        /// Function to select shape while switch from quick tool pallete
        /// </summary>
        public void QuickShapeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            object ToolPaletteShape = (sender as System.Windows.Controls.Button).Name == "btnQuickRect" ? SelectionRectangle :
                                        (sender as System.Windows.Controls.Button).Name == "btnQuickCircle" ? SelectionCircle :
                                        (sender as System.Windows.Controls.Button).Name == "btnQuickPoly" ? SelectionPoly :
                                        (sender as System.Windows.Controls.Button).Name == "btnQuickSelect" ? SelectArrow : null;

            SelectedShape = (sender as System.Windows.Controls.Button).Name == "btnQuickRect" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Rectangle :
                            (sender as System.Windows.Controls.Button).Name == "btnQuickCircle" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Circle :
                            (sender as System.Windows.Controls.Button).Name == "btnQuickPoly" && (sender as System.Windows.Controls.Button).IsEnabled ? EnumSelectedShape.Polyline : EnumSelectedShape.Null;

            SetShapeVisibilityWhichSwitch();            //Set shape visible or not while changing shape tool
            SetShapeButtonFocus(sender, ToolPaletteShape);

            if (selShape != null && (!bIsClassAdded || !IsPolyFirstPoint))
            {
                drawingSurface.Children.Remove(selShape);                    //removes previously drawn shape
                if (selLineShape != null)
                    drawingSurface.Children.Remove(selLineShape);
                IsEnableClassStackPanel = false;
                IsPolyFirstPoint = true;
            }
        }

        /// <summary>
        /// Function to set focus on selected shape tool and remove from others
        /// </summary>
        private void SetShapeButtonFocus(object QuickPalletShape, object ToolPalletShape)
        {
            for (int i = 0; i < spQuickTool.Children.Count; i++) 
                (spQuickTool.Children[i] as System.Windows.Controls.Button).Background = null;
            
            if(QuickPalletShape != null)
                (QuickPalletShape as System.Windows.Controls.Button).Background = (QuickPalletShape as System.Windows.Controls.Button).IsEnabled ? (Brush)(new BrushConverter().ConvertFrom("#FFD9B9F9")) : null;

            for (int i = 0; i < PanelTools.Children.Count; i++) 
                (PanelTools.Children[i] as System.Windows.Controls.Button).Background = null;

            if (ToolPalletShape != null)
                (ToolPalletShape as System.Windows.Controls.Button).Background = (ToolPalletShape as System.Windows.Controls.Button).IsEnabled ? (Brush)(new BrushConverter().ConvertFrom("#FFD9B9F9")) : null;

            copyShape = null;
            this.Cursor = System.Windows.Input.Cursors.Arrow;
            if(!bIsListClassSelected)
                ListViewClass_LostFocus(null, null);
        }

        /// <summary>
        /// Function to set visibility of shape tool while changing shape menu from side panel or quick tool pallete
        /// </summary>
        private void SetShapeVisibilityWhichSwitch()
        {
            //var filteredCanvasChilds = drawingSurface.Children.OfType<UIElement>().Where(child => !(child is System.Windows.Controls.Label)).ToList();
            //if (filteredCanvasChilds.Count > 1)
            //{
            //    if (SelectedShape == EnumSelectedShape.Null)
            //    {
            //        for (int nChild = 1; nChild < filteredCanvasChilds.Count; nChild++)
            //        {
            //            Shape curShape = filteredCanvasChilds[nChild] as Shape;
            //            curShape.Visibility = Visibility.Visible;
            //        }
            //    }
            //    else if (SelectedShape == EnumSelectedShape.Rectangle)
            //    {
            //        for (int nChild = 1; nChild < filteredCanvasChilds.Count; nChild++)
            //        {
            //            Shape curShape = filteredCanvasChilds[nChild] as Shape;
            //            curShape.Visibility = curShape.DependencyObjectType.Name == "Rectangle" ? Visibility.Visible : Visibility.Collapsed;
            //        }
            //    }
            //    else if (SelectedShape == EnumSelectedShape.Polyline)
            //    {
            //        for (int nChild = 1; nChild < filteredCanvasChilds.Count; nChild++)
            //        {
            //            Shape curShape = filteredCanvasChilds[nChild] as Shape;
            //            curShape.Visibility = curShape.DependencyObjectType.Name == "Polyline" ? Visibility.Visible : Visibility.Collapsed;
            //        }
            //    }
            //}

            if (SelectedImageBox != null)
            {
                foreach (ImageClass curClass in SelectedImageBox.ListImageClass)
                {
                    int Shapeindex = drawingSurface.Children.IndexOf(curClass.DrawShape);
                    int lblIndex = drawingSurface.Children.IndexOf(curClass.DrawLabel);
                    if (SelectedShape == EnumSelectedShape.Null)
                    {
                        if (Shapeindex != -1 && Shapeindex < drawingSurface.Children.Count)
                            drawingSurface.Children[Shapeindex].Visibility = Visibility.Visible;

                        if (lblIndex != -1 && lblIndex < drawingSurface.Children.Count)
                            drawingSurface.Children[lblIndex].Visibility = Visibility.Visible;
                    }

                    if (SelectedShape == EnumSelectedShape.Rectangle)
                    {
                        if (Shapeindex != -1 && Shapeindex < drawingSurface.Children.Count)
                            drawingSurface.Children[Shapeindex].Visibility = curClass.Shape == EnumSelectedShape.Rectangle ? Visibility.Visible : Visibility.Collapsed;

                        if (lblIndex != -1 && lblIndex < drawingSurface.Children.Count)
                            drawingSurface.Children[lblIndex].Visibility = curClass.Shape == EnumSelectedShape.Rectangle ? Visibility.Visible : Visibility.Collapsed;
                    }

                    if (SelectedShape == EnumSelectedShape.Circle)
                    {
                        if (Shapeindex != -1 && Shapeindex < drawingSurface.Children.Count)
                            drawingSurface.Children[Shapeindex].Visibility = curClass.Shape == EnumSelectedShape.Circle ? Visibility.Visible : Visibility.Collapsed;

                        if (lblIndex != -1 && lblIndex < drawingSurface.Children.Count)
                            drawingSurface.Children[lblIndex].Visibility = curClass.Shape == EnumSelectedShape.Circle ? Visibility.Visible : Visibility.Collapsed;
                    }

                    if (SelectedShape == EnumSelectedShape.Polyline)
                    {
                        if (Shapeindex != -1 && Shapeindex < drawingSurface.Children.Count)
                            drawingSurface.Children[Shapeindex].Visibility = curClass.Shape == EnumSelectedShape.Polyline ? Visibility.Visible : Visibility.Collapsed;

                        if (lblIndex != -1 && lblIndex < drawingSurface.Children.Count)
                            drawingSurface.Children[lblIndex].Visibility = curClass.Shape == EnumSelectedShape.Polyline ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        private void AddClassButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (cmbClassName.SelectedItem != null)
            {
                flagClassOp = (sender as System.Windows.Controls.Button).Content.ToString() == "Add Class" ? 'A' : 'E';
                AddClasstoListView();
            }
            else
                cmbClassName.Focus();
        }

        /// <summary>
        /// Function to add the annotated shape and classes to the images and Edit the already drawn shape and classes
        /// </summary>
        private void AddClasstoListView()
        {
            try
            {
                if (flagClassOp == 'A')                 //To new class to add
                {
                    ImageClass curClassAttribute = new ImageClass(txtClassID.Text.ToString(), cmbClassName.SelectedItem.ToString());
                    EnumSelectedShape curEnumShape;
                    if (selShape.DependencyObjectType.Name != "")
                        curEnumShape = selShape.DependencyObjectType.Name == "Rectangle" ? EnumSelectedShape.Rectangle : selShape.DependencyObjectType.Name == "Ellipse" ?
                            EnumSelectedShape.Circle : selShape.DependencyObjectType.Name == "Polyline" ? EnumSelectedShape.Polyline : EnumSelectedShape.Null;
                    else
                        curEnumShape = SelectedShape;

                    string shape = (curEnumShape == EnumSelectedShape.Rectangle) ? "rect" : (curEnumShape == EnumSelectedShape.Circle) ? "circle" :
                                    (curEnumShape == EnumSelectedShape.Polyline) ? "polyline" : "";

                    if (curEnumShape == EnumSelectedShape.Rectangle || curEnumShape == EnumSelectedShape.Circle)
                    {
                        curClassAttribute.XCoordinate = Math.Round(Canvas.GetLeft(selShape), 3);
                        curClassAttribute.YCoordinate = Math.Round(Canvas.GetTop(selShape), 3);
                        curClassAttribute.Width = Math.Round(selShape.Width, 3);
                        curClassAttribute.Height = Math.Round(selShape.Height, 3);
                        curClassAttribute.All_Points_X = new List<double>();
                        curClassAttribute.All_Points_Y = new List<double>();
                        curClassAttribute.ShapeCoordinates = "{\"name\":\"" + shape + "\", \"x\": " + curClassAttribute.XCoordinate + ", \"y\": " + curClassAttribute.YCoordinate +
                                                            ", \"width\": " + curClassAttribute.Width + ", \"height\": " + curClassAttribute.Height + " }";
                        curClassAttribute.Shape = curEnumShape;
                    }

                    else if (curEnumShape == EnumSelectedShape.Polyline)
                    {
                        curClassAttribute.XCoordinate = 0;
                        curClassAttribute.YCoordinate = 0;
                        curClassAttribute.Width = 0;
                        curClassAttribute.Height = 0;
                        foreach (Point p in (selShape as Polyline).Points)
                        {
                            curClassAttribute.All_Points_X.Add(Math.Round(p.X, 0));
                            curClassAttribute.All_Points_Y.Add(Math.Round(p.Y, 0));
                        }
                        curClassAttribute.ShapeCoordinates = "{\"name\":\"" + shape + "\", \"all_points_x\": [" + String.Join(", ", curClassAttribute.All_Points_X) + "], \"all_points_y\": [" + String.Join(", ", curClassAttribute.All_Points_Y) + "] }";
                        curClassAttribute.Shape = curEnumShape;
                    }

                    curClassAttribute.ClassAlias = cmbClassName.SelectedItem.ToString().Split('(', ')').Length > 1 ? cmbClassName.SelectedItem.ToString().Split('(', ')')[1]
                                    : cmbClassName.SelectedItem.ToString().Split('(', ')')[0];

                    SelectedImageBox.ListImageClass.Insert(0, curClassAttribute);
                    if (!ProcessedImageBox.Contains(SelectedImageBox))
                    {
                        ProcessedImageBox.Add(SelectedImageBox);
                        RefreshListBoxImages();
                    }

                    Shape curShape = drawingSurface.Children[drawingSurface.Children.Count - 1] as Shape;
                    curShape.ToolTip = curClassAttribute.ClassAlias;
                    System.Windows.Controls.Label ROILabel = GetROILabel(curClassAttribute);
                    curClassAttribute.DrawLabel = ROILabel;
                    curClassAttribute.DrawShape = curShape;
                    drawingSurface.Children.Add(ROILabel);
                    //UndoRedoItem undoRedoItem = new UndoRedoItem();
                    //undoRedoItem.Type = "Add";
                    //undoRedoItem.listObjects.Add(curShape);
                    //undoRedoItem.listObjects.Add(curClassAttribute);
                    //undoRedo.InsertUndoStack(undoRedoItem);

                    bIsSaved = false;
                    txtClassID.Text = "";
                    cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                    cmbClassName.Text = "";
                    cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                    IsEnableClassStackPanel = false;
                    bIsClassAdded = true;
                    //UndoButton.IsEnabled = true;
                    lvImageClass.SelectedIndex = SelectedImageBox.ListImageClass.IndexOf(curClassAttribute);
                    lvImageClass.Focus();
                    selShape = null;
                }
                else if (flagClassOp == 'E' && ResizeImageClass != null && resizeShape != null)          //to edit existing class
                {
                    ResizeImageClass.ClassIndex = txtClassID.Text.ToString();
                    ResizeImageClass.ClassName = cmbClassName.SelectedItem.ToString();
                    ResizeImageClass.ClassAlias = cmbClassName.SelectedItem.ToString().Split('(', ')').Length > 1 ? cmbClassName.SelectedItem.ToString().Split('(', ')')[1]
                                                            : cmbClassName.SelectedItem.ToString().Split('(', ')')[0];
                    resizeShape.ToolTip = ResizeImageClass.ClassAlias;
                    int index = drawingSurface.Children.IndexOf(ResizeImageClass.DrawLabel);

                    if(index != -1)
                    {
                        System.Windows.Controls.Label ROILabel = drawingSurface.Children[index] as System.Windows.Controls.Label;
                        ROILabel.Content = ResizeImageClass.ClassAlias;
                    }

                    string strAlias = "";
                    if (arrSeleImportData != null && arrSeleImportData.Length > 3)
                    {
                        strAlias = Regex.Match(Regex.Replace(arrSeleImportData[3], @"[""{}]", ""), @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                        var curItem = ListModifiedClass.FirstOrDefault(item => item.ModifiedClassName.ToUpper() == ResizeImageClass.ClassAlias.ToUpper());
                        string classID = curItem != null? curItem.ModifiedID : ResizeImageClass.ClassIndex;
                        string strRegion = "{\"class id\":\"" + classID + "\", \"class name\":\"" + ResizeImageClass.ClassAlias + "\"}";
                        arrSeleImportData[3] = "\"" + strRegion.Replace("\"", "\"\"") + "\"";

                        char strImageType = SelectedImageBox.ImageBoxName.Contains(settings.SinglePhase) ? 'S' : SelectedImageBox.ImageBoxName.Contains(settings.PhaseContrast) ? 'P' : ' ';
                        var curClassStat = ListClassFolderStat.FirstOrDefault(item => item.ImportDatasheetName == ResizeImageClass.ImportDatasheetName && item.ClassAliasName.ToUpper() == strAlias);
                        if (curClassStat != null)
                        {
                            curClassStat.ClassCount--;
                            if (strImageType == 'S')
                                curClassStat.SingleSpotCount--;
                            else if (strImageType == 'P')
                                curClassStat.PhaseContrastCount--;
                        }

                        var newClassStat = ListClassFolderStat.FirstOrDefault(item => item.ImportDatasheetName == ResizeImageClass.ImportDatasheetName && item.ClassAliasName.ToUpper() == ResizeImageClass.ClassAlias.ToUpper());
                        if (newClassStat != null){
                            newClassStat.ClassCount++;
                            if (strImageType == 'S')
                                newClassStat.SingleSpotCount++;
                            else if (strImageType == 'P')
                                newClassStat.PhaseContrastCount++;
                        }
                        else{
                            ListClassFolderStat.Add(new ClassFolderStat
                            {
                                ImportDatasheetName = ResizeImageClass.ImportDatasheetName,
                                ClassAliasName = ResizeImageClass.ClassAlias,
                                ClassID = ResizeImageClass.ClassIndex,
                                ClassCount = 1,
                                SingleSpotCount = strImageType == 'S' ? 1 : 0,
                                PhaseContrastCount = strImageType == 'P' ? 1 : 0
                            });
                        }
                    }

                    txtClassID.Text = "";
                    cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                    cmbClassName.Text = "";
                    cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                    IsEnableClassStackPanel = false;
                    btnAddClass.Content = "Add Class";
                    btnAddClass.ToolTip = "Add to Class";
                    lvImageClass.Items.Refresh();
                    resizeShape = null;
                    lvImageClass.SelectedIndex = SelectedImageBox.ListImageClass.IndexOf(ResizeImageClass);
                    lvImageClass.Focus();
                }
                else if(flagClassOp == 'S')
                {
                    if (SelectedImageBox.ListImageClass.Any(item => item.ClassName == cmbClassName.SelectedItem.ToString()))
                    {
                        System.Windows.MessageBox.Show("Image already segregated with this class..!", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        txtClassID.Text = "";
                        cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                        cmbClassName.Text = "";
                        cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                        IsEnableClassStackPanel = false;
                        return;
                    }

                    ImageClass curClassAttribute = new ImageClass(txtClassID.Text.ToString(), cmbClassName.SelectedItem.ToString());          
                    curClassAttribute.ClassAlias = cmbClassName.SelectedItem.ToString().Split('(', ')').Length > 1 ? cmbClassName.SelectedItem.ToString().Split('(', ')')[1]
                                                    : cmbClassName.SelectedItem.ToString().Split('(', ')')[0];
                    
                    SelectedImageBox.ListImageClass.Insert(0, curClassAttribute);
                    if (!ProcessedImageBox.Contains(SelectedImageBox))
                    {
                        ProcessedImageBox.Add(SelectedImageBox);
                        RefreshListBoxImages();
                    }

                    txtClassID.Text = "";
                    cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                    cmbClassName.Text = "";
                    cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                    IsEnableClassStackPanel = false;
                    lvImageClass.SelectedIndex = SelectedImageBox.ListImageClass.IndexOf(curClassAttribute);
                    lvImageClass.Focus();
                }
                else if(flagClassOp == 'V' && ResizeImageClass != null)
                {
                    if (SelectedImageBox.ListImageClass.Any(item => item.ClassName == cmbClassName.SelectedItem.ToString()))
                    {
                        System.Windows.MessageBox.Show("Image already segregated with this class..!", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        txtClassID.Text = "";
                        cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                        cmbClassName.Text = "";
                        cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                        IsEnableClassStackPanel = false;
                        return;
                    }

                    ResizeImageClass.ClassIndex = txtClassID.Text.ToString();
                    ResizeImageClass.ClassName = cmbClassName.SelectedItem.ToString();
                    ResizeImageClass.ClassAlias = cmbClassName.SelectedItem.ToString().Split('(', ')').Length > 1 ? cmbClassName.SelectedItem.ToString().Split('(', ')')[1]
                                                            : cmbClassName.SelectedItem.ToString().Split('(', ')')[0];

                    txtClassID.Text = "";
                    cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                    cmbClassName.Text = "";
                    cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                    IsEnableClassStackPanel = false;
                    lvImageClass.Items.Refresh();
                    lvImageClass.SelectedIndex = SelectedImageBox.ListImageClass.IndexOf(ResizeImageClass);
                    lvImageClass.Focus();
                }
            }

            catch (System.Exception ex)
            {
                Utilities.LogMessage("MainWindow::AddClasstoListView: " + ex.Message);
            }
        }

        /// <summary>
        /// Function to call Reset Images window 
        /// </summary>
        public void ResetWindow()
        {
            StatusBarImageFile = "";
            StatusBarDimension = "";
            StatusSelectedDimension = "";
            ImageSource.Source = null;
            lvImageClass.ItemsSource = null;
            StatusNoteVisiblity = Visibility.Collapsed;
            UndoButton.IsEnabled = false;
            RedoButton.IsEnabled = false;
            undoRedo.Clear();
            IsEnableClassStackPanel = false;
            if (drawingSurface.Children.Count > 1)
                drawingSurface.Children.RemoveRange(1, drawingSurface.Children.Count - 1);   //not removes first child i,e image
            resizeShape = null;
            ResizeImageClass = null;
            arrSeleImportData = null;
            bIsClassAdded = true;
            IsPolyFirstPoint = true;
            lblZoomStatus.Visibility = Visibility.Collapsed;
            selBoundBox = null;
        }

        /// <summary>
        /// Function to call the event for Keyboard shortcut operations, ESC operations to clear drawn shapes
        /// </summary>
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && (e.Key == Key.Left || e.Key == Key.A) && listBoxImages.SelectedItem != null && !IsEnableClassStackPanel && tabSideBar.SelectedIndex == 0)
            {
                if (listBoxImages.SelectedIndex > 0)
                {
                    listBoxImages.SelectedIndex--;
                    listBoxImages.ScrollIntoView(listBoxImages.Items[listBoxImages.SelectedIndex]);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && (e.Key == Key.Right || e.Key == Key.D) && listBoxImages.SelectedItem != null && !IsEnableClassStackPanel && tabSideBar.SelectedIndex == 0)
            {
                if (listBoxImages.SelectedIndex < listBoxImages.Items.Count)
                {
                    listBoxImages.SelectedIndex++;
                    listBoxImages.ScrollIntoView(listBoxImages.Items[listBoxImages.SelectedIndex]);
                }
            }
            else if (e.Key == Key.Left && listAnalysisImages.SelectedItem != null && tabSideBar.SelectedIndex == 4)
            {
                if (listAnalysisImages.SelectedIndex > 0)
                {
                    listAnalysisImages.SelectedIndex--;
                    listAnalysisImages.ScrollIntoView(listAnalysisImages.Items[listAnalysisImages.SelectedIndex]);
                }
            }
            else if (e.Key == Key.Right && listAnalysisImages.SelectedItem != null && tabSideBar.SelectedIndex == 4)
            {
                if (listAnalysisImages.SelectedIndex < listAnalysisImages.Items.Count)
                {
                    listAnalysisImages.SelectedIndex++;
                    listAnalysisImages.ScrollIntoView(listAnalysisImages.Items[listAnalysisImages.SelectedIndex]);
                }
            }

            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.O) || e.Key == Key.F2)
                LoadImageFolder_Click(OpenFolderButton, null);

            else if ((((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.I) || e.Key == Key.F4) && settings.ClassType != EnumClassType.Segregation)
                ImportMultipleCSV_Click(ImportMultitButton, null);

            else if ((((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.I) || e.Key == Key.F4) && settings.ClassType == EnumClassType.Segregation)
                ImportCSV_Click(ImportButton, null);

            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.X) || e.Key == Key.F6)
                CSVExport_Click(ExportButton, null);

            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.M) || e.Key == Key.F5)
                ImportMultipleJSON_Click(ImportJasontButton, null);

            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.J) || e.Key == Key.F7)
                JSONExport_Click(ExportJasontButton, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.A)
                AddImageAttributeToClass_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
                CopyImageAttribute_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
                PasteImageAttribute_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.E)
                EditImageAttribute_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
                SaveROItoDisk_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.B)
                ShowBoundingBox_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z)
                UndoChangestoCanvas_Click(null, null);

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Y)
                RedoChanges_Click(null, null);

            else if (e.Key == Key.F)
            {
                ResetImageWindow(drawingSurface, 'F');
            }

            else if (e.Key == Key.Escape)
            {
                if (cmbClassName.IsDropDownOpen)
                {
                    cmbClassName.IsDropDownOpen = false;
                    IsEnableClassStackPanel = false;
                    btnAddClass.Content = "Add Class";
                    btnAddClass.ToolTip = "Add to Class";
                }
                if (selLineShape != null && selLineShape.DependencyObjectType.Name == "Line")
                    drawingSurface.Children.Remove(selLineShape);
                if (selShape != null && selShape.DependencyObjectType.Name == "Polyline" && bIsClassAdded)
                {
                    Polyline curPolyShape = selShape as Polyline;
                    if (curPolyShape.Points.Count > 0)
                    {
                        curPolyShape.Points.RemoveAt(curPolyShape.Points.Count - 1);
                        if (curPolyShape.Points.Count == 0)
                        {
                            drawingSurface.Children.Remove(selShape);
                            selShape = null;
                            bIsClassAdded = true;
                            IsPolyFirstPoint = true;
                            this.Cursor = System.Windows.Input.Cursors.Arrow;
                        }
                    }
                }
            }

            else if(e.Key == Key.Q && settings.ClassType == EnumClassType.Rectangle && ImageSource.Source != null 
                && SelectedShape != EnumSelectedShape.Rectangle && txtSearchText.IsFocused == false)
            {
                QuickShapeButton_MouseDown(btnQuickRect, null);
                lblZoomStatus.Content = "Rectangle tool Selected";
                lblZoomStatus.Visibility = Visibility.Visible;
                System.Windows.Media.Animation.Storyboard sb = this.Resources["sbHideZoomLabel"] as System.Windows.Media.Animation.Storyboard;
                sb.Begin(lblZoomStatus);

            }
            else if (e.Key == Key.W && settings.ClassType == EnumClassType.Any && ImageSource.Source != null 
                && SelectedShape != EnumSelectedShape.Polyline && txtSearchText.IsFocused == false)
            {
                QuickShapeButton_MouseDown(btnQuickPoly, null);
                lblZoomStatus.Content = "Polyline tool Selected";
                lblZoomStatus.Visibility = Visibility.Visible;
                System.Windows.Media.Animation.Storyboard sb = this.Resources["sbHideZoomLabel"] as System.Windows.Media.Animation.Storyboard;
                sb.Begin(lblZoomStatus);
            }
            else if (e.Key == Key.O && settings.ClassType == EnumClassType.Any && ImageSource.Source != null
                && SelectedShape != EnumSelectedShape.Circle && txtSearchText.IsFocused == false)
            {
                QuickShapeButton_MouseDown(btnQuickCircle, null);
                lblZoomStatus.Content = "Circle tool Selected";
                lblZoomStatus.Visibility = Visibility.Visible;
                System.Windows.Media.Animation.Storyboard sb = this.Resources["sbHideZoomLabel"] as System.Windows.Media.Animation.Storyboard;
                sb.Begin(lblZoomStatus);
            }
            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.R))
                ResetAll_Click(null, null);
        }

        /// <summary>
        /// Function to Operates when Added class item list selection changed eg, to highlight the selected class shape
        /// </summary>
        private void ListViewItemClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.Source is System.Windows.Controls.ListView))
                return;

            if (lvImageClass.SelectedIndex == -1 || settings.ClassType == EnumClassType.Segregation){
                StatusSelectedDimension = "";
                return;
            }

            try
            {
                var filteredCanvasChilds = drawingSurface.Children.OfType<UIElement>().Where(child => !(child is System.Windows.Controls.Label)).ToList();
                for (int i = 1; i < filteredCanvasChilds.Count; i++)
                {
                    Shape curShapes = filteredCanvasChilds[i] as Shape;
                    //curShapes.Stroke = Brushes.Red;
                    ImageClass tempClass = GetResizeImageClass(curShapes);
                    curShapes.Stroke = tempClass != null ? tempClass.HighLightStroke : Brushes.Red;
                    curShapes.Fill = new SolidColorBrush { Color = Colors.Transparent, Opacity = 1 };
                }

                Shape curShape = filteredCanvasChilds[lvImageClass.Items.Count - lvImageClass.SelectedIndex] as Shape;
                resizeShape = curShape;
                ResizeImageClass = GetResizeImageClass(resizeShape);

                if (ResizeImageClass != null)
                {
                    curShape.Fill = new SolidColorBrush { Color = Colors.Aqua, Opacity = 0.1 };
                    curShape.Stroke = ResizeImageClass.SelectionStroke;
                    bIsListClassSelected = true;

                    if (ResizeImageClass.Shape == EnumSelectedShape.Rectangle && SelectedShape != EnumSelectedShape.Null)
                        QuickShapeButton_MouseDown(btnQuickRect, null);
                    else if (ResizeImageClass.Shape == EnumSelectedShape.Polyline && SelectedShape != EnumSelectedShape.Null)
                        QuickShapeButton_MouseDown(btnQuickPoly, null);
                    else if (ResizeImageClass.Shape == EnumSelectedShape.Circle && SelectedShape != EnumSelectedShape.Null)
                        QuickShapeButton_MouseDown(btnQuickCircle, null);

                    arrSeleImportData = null;
                    var tempImportData = ListDatasheetImportData.FirstOrDefault(temp => temp.DatasheetName == ResizeImageClass.ImportDatasheetName);
                    if (tempImportData != null)
                        arrSeleImportData = tempImportData.ListImportData.FirstOrDefault(item => item[0].Trim() == SelectedImageBox.ImageBoxName && Regex.Replace(item[2], @"[""{} ]", "") == Regex.Replace(ResizeImageClass.ShapeCoordinates, @"[""{} ]", ""));
                }
                else
                {
                    curShape.Stroke = Brushes.Blue;
                    curShape.Fill = new SolidColorBrush { Color = Colors.Transparent, Opacity = 1 };
                }

                bIsListClassSelected = false;
                if (curShape.Width > 0 & curShape.Height > 0)
                    if (curShape.DependencyObjectType.Name == "Ellipse")
                        StatusSelectedDimension = Convert.ToInt32(curShape.Width) + " x " + Convert.ToInt32(curShape.Height) + "    rad : " + (int)(curShape.Width / 2);
                    else
                        StatusSelectedDimension = Convert.ToInt32(curShape.Width) + " x " + Convert.ToInt32(curShape.Height);
                else
                    StatusSelectedDimension = "";
            }

            catch (Exception)
            {
            }
        }


        private void ListViewClass_LostFocus(object sender, RoutedEventArgs e)
        {
            var filteredCanvasChilds = drawingSurface.Children.OfType<UIElement>().Where(child => !(child is System.Windows.Controls.Label)).ToList();
            for (int i = 1; i < filteredCanvasChilds.Count; i++)
            {
                Shape curShapes = filteredCanvasChilds[i] as Shape;
                //curShapes.Stroke = Brushes.Red;
                ImageClass tempClass = GetResizeImageClass(curShapes);
                curShapes.Stroke = tempClass != null ? tempClass.HighLightStroke : Brushes.Red;
                curShapes.Fill = new SolidColorBrush { Color = Colors.Transparent, Opacity = 1 };
            }

            lvImageClass.SelectionChanged -= ListViewItemClass_SelectionChanged;
            lvImageClass.SelectedIndex = -1;
            lvImageClass.SelectionChanged += ListViewItemClass_SelectionChanged;
            StatusSelectedDimension = "";
        }

        /// <summary>
        /// Function to open when Setttings menu click which opens Configuration window
        /// </summary>
        private void SettingsWindow_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow windowSettings = new ConfigurationWindow(this);
            windowSettings.Owner = this;
            windowSettings.ShowDialog();
            UpdateNotifyPropertyChnages();
        }

        /// <summary>
        /// Function to start Save Work menu was selected
        /// </summary>
        private void SaveProcessedWork_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;
            if (ImageMenuList.Count == 0 || ProcessedImageBox.Count == 0)
            {
                System.Windows.MessageBox.Show("No work found to save..", "No work", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            bool triggered = SaveEvent.WaitOne(10);
            if (!triggered)
            {
                System.Windows.MessageBox.Show("Could not save work for a moment..!\nWork Save is already in progress..", "In Progress", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to Save work done?", "Save Work", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_SaveProcessedImageStat;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStart("Save");
        }

        /// <summary>
        /// Function to delete selected class and drawn shape from images and class list
        /// </summary>
        private void ListClassView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
                e.Handled = true;

            if ((e.Key == Key.Delete || e.Key == Key.Space) && lvImageClass.SelectedIndex != -1 && lvImageClass.SelectedIndex < lvImageClass.Items.Count && settings.ClassType != EnumClassType.Segregation)
            {
                ImageClass curClassImage = lvImageClass.SelectedItem as ImageClass;

                string strAlias = "";
                int arrIndex = -1;
                if (arrSeleImportData != null)
                {
                    strAlias = Regex.Match(Regex.Replace(arrSeleImportData[3], @"[""{}]", ""), @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                    var tempImportData = ListDatasheetImportData.FirstOrDefault(temp => temp.DatasheetName == curClassImage.ImportDatasheetName);
                    if (tempImportData != null)
                    {
                        arrIndex = tempImportData.ListImportData.IndexOf(arrSeleImportData);
                        tempImportData.ListImportData.Remove(arrSeleImportData);
                    }

                    char strImageType = SelectedImageBox.ImageBoxName.Contains(settings.SinglePhase) ? 'S' : SelectedImageBox.ImageBoxName.Contains(settings.PhaseContrast) ? 'P' : ' ';
                    var curClassStat = ListClassFolderStat.FirstOrDefault(item => item.ImportDatasheetName == ResizeImageClass.ImportDatasheetName && item.ClassAliasName.ToUpper() == strAlias);
                    if (curClassStat != null)
                    {
                        curClassStat.ClassCount--;
                        if (strImageType == 'S')
                            curClassStat.SingleSpotCount--;
                        else if (strImageType == 'P')
                            curClassStat.PhaseContrastCount--;
                    }
                }

                //Shape curShape = drawingSurface.Children[lvImageClass.Items.Count - lvImageClass.SelectedIndex] as Shape;
                //drawingSurface.Children.RemoveAt(lvImageClass.Items.Count - lvImageClass.SelectedIndex);
                if(curClassImage != null)
                {
                    drawingSurface.Children.Remove(curClassImage.DrawShape);
                    drawingSurface.Children.Remove(curClassImage.DrawLabel);
                }
                SelectedImageBox.ListImageClass.Remove(curClassImage);

                if (SelectedImageBox.ListImageClass.Count == 0)
                    ProcessedImageBox.Remove(SelectedImageBox);

                UndoRedoItem undoRedoItem = new UndoRedoItem();
                undoRedoItem.Type = "Del";
                undoRedoItem.listObjects.Add(curClassImage.DrawShape);
                undoRedoItem.listObjects.Add(curClassImage);
                if (arrSeleImportData != null)
                {
                    undoRedoItem.listObjects.Add(arrIndex);
                    undoRedoItem.listObjects.Add(arrSeleImportData);
                }

                undoRedo.InsertUndoStack(undoRedoItem);

                UndoButton.IsEnabled = true;
                bIsSaved = false;
                RefreshListBoxImages();
            }
            else if (e.Key == Key.Delete && lvImageClass.SelectedIndex == -1 && resizeShape != null && ResizeImageClass == null && settings.ClassType != EnumClassType.Segregation)
            {
                var filteredCanvasChilds = drawingSurface.Children.OfType<UIElement>().Where(child => !(child is System.Windows.Controls.Label)).ToList();
                filteredCanvasChilds.Remove(resizeShape);
                bIsClassAdded = true;
            }
            else if(e.Key == Key.Delete && lvImageClass.SelectedIndex != -1 && lvImageClass.SelectedIndex < lvImageClass.Items.Count && settings.ClassType == EnumClassType.Segregation)
            {
                ImageClass curClassImage = lvImageClass.SelectedItem as ImageClass;
                SelectedImageBox.ListImageClass.Remove(curClassImage);

                if (SelectedImageBox.ListImageClass.Count == 0)
                    ProcessedImageBox.Remove(SelectedImageBox);

                RefreshListBoxImages();
            }
        }

        private void UndoChangestoCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (undoRedo.CanUndo())
            {
                Utilities.LogMessage(undoRedo.UndoCount().ToString());
                UndoRedoItem undoItem = undoRedo.UndoObject as UndoRedoItem;
                if(undoItem.Type == "Add")
                {
                    Shape curShape = undoItem.listObjects.First() as Shape;
                    ImageClass curImageClass = undoItem.listObjects.Last() as ImageClass;
                    drawingSurface.Children.Remove(curShape);
                    if(curImageClass != null && curImageClass.DrawLabel == null)
                        drawingSurface.Children.Remove(curImageClass.DrawLabel);
                    SelectedImageBox.ListImageClass.Remove(curImageClass);

                    UndoRedoItem redoItem = new UndoRedoItem();
                    redoItem.Type = "Del";
                    redoItem.listObjects.Add(curShape);
                    redoItem.listObjects.Add(curImageClass);
                    undoRedo.InsertRedoStack(redoItem);
                    IsEnableClassStackPanel = false;
                    RedoButton.IsEnabled = true;
                    bIsSaved = false;
                }
                else if(undoItem.Type == "Del")
                {
                    Shape curShape = undoItem.listObjects.Count > 0? undoItem.listObjects[0] as Shape : null;
                    ImageClass curImageClass = undoItem.listObjects.Count > 1 ? undoItem.listObjects[1] as ImageClass : null;

                    if (curShape != null && drawingSurface.Children.Contains(curShape))
                        return;

                    if (curShape != null)                   
                        drawingSurface.Children.Add(curShape);

                    if (curImageClass != null)
                    {
                        SelectedImageBox.ListImageClass.Insert(0, curImageClass);
                        if(curImageClass.DrawLabel != null)
                            drawingSurface.Children.Add(curImageClass.DrawLabel);
                    }

                    string[] arrSeleImportData = null;
                    int arrIndex = -1;
                    if (undoItem.listObjects.Count > 2)
                    {
                        arrIndex = (int)undoItem.listObjects[2];
                        arrSeleImportData = ((IEnumerable)undoItem.listObjects[3]).Cast<object>()
                                                    .Select(x => x.ToString())
                                                    .ToArray();

                        string strAlias = Regex.Match(Regex.Replace(arrSeleImportData[3], @"[""{}]", ""), @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                        var tempImportData = ListDatasheetImportData.FirstOrDefault(temp => temp.DatasheetName == curImageClass.ImportDatasheetName);
                        if (tempImportData != null)
                            tempImportData.ListImportData.Insert(arrIndex, arrSeleImportData);

                        char strImageType = SelectedImageBox.ImageBoxName.Contains(settings.SinglePhase) ? 'S' : SelectedImageBox.ImageBoxName.Contains(settings.PhaseContrast) ? 'P' : ' ';
                        var curClassStat = ListClassFolderStat.FirstOrDefault(item => item.ImportDatasheetName == curImageClass.ImportDatasheetName && item.ClassAliasName.ToUpper() == strAlias);
                        if (curClassStat != null)
                        {
                            curClassStat.ClassCount++;
                            if (strImageType == 'S')
                                curClassStat.SingleSpotCount++;
                            else if (strImageType == 'P')
                                curClassStat.PhaseContrastCount++;
                        }
                    }                        

                    UndoRedoItem redoItem = new UndoRedoItem();
                    redoItem.Type = "Add";
                    redoItem.listObjects.Add(curShape);
                    redoItem.listObjects.Add(curImageClass);
                    if (arrSeleImportData != null)
                    {
                        redoItem.listObjects.Add(arrIndex);
                        redoItem.listObjects.Add(arrSeleImportData);
                    }

                    undoRedo.InsertRedoStack(redoItem);
                    IsEnableClassStackPanel = false;
                    RedoButton.IsEnabled = true;
                    bIsSaved = false;
                }

                var filteredCanvasChilds = drawingSurface.Children.OfType<UIElement>().Where(child => !(child is System.Windows.Controls.Label)).ToList();
                for (int i = 1; i < filteredCanvasChilds.Count; i++)
                {
                    Shape curShapes = filteredCanvasChilds[i] as Shape;
                    //curShapes.Stroke = Brushes.Red;
                    ImageClass tempClass = GetResizeImageClass(curShapes);
                    curShapes.Stroke = tempClass != null ? tempClass.HighLightStroke : Brushes.Red;
                    curShapes.Fill = new SolidColorBrush { Color = Colors.Transparent, Opacity = 1 };
                }
                if (undoRedo.CanUndo())
                    UndoButton.IsEnabled = true;
                else
                    UndoButton.IsEnabled = false;
            }
        }

        private void RedoChanges_Click(object sender, RoutedEventArgs e)
        {
            if (undoRedo.CanRedo()) 
            {
                Utilities.LogMessage(undoRedo.RedoCount().ToString());
                UndoRedoItem redoItem = undoRedo.RedoObject as UndoRedoItem;
                if (redoItem.Type == "Del")
                {
                    Shape curShape = redoItem.listObjects.First() as Shape;
                    ImageClass curImageClass = redoItem.listObjects.Last() as ImageClass;

                    drawingSurface.Children.Add(curShape);
                    if (curImageClass != null && curImageClass.DrawLabel == null)
                        drawingSurface.Children.Add(curImageClass.DrawLabel);
                    SelectedImageBox.ListImageClass.Insert(0, curImageClass);

                    UndoRedoItem undoItem = new UndoRedoItem();
                    undoItem.Type = "Add";
                    undoItem.listObjects.Add(curShape);
                    undoItem.listObjects.Add(curImageClass);
                    undoRedo.InsertUndoStack(undoItem);
                    IsEnableClassStackPanel = false;
                    UndoButton.IsEnabled = true;
                    bIsSaved = false;
                }
                else if (redoItem.Type == "Add")
                {
                    Shape curShape = redoItem.listObjects.Count > 0 ? redoItem.listObjects[0] as Shape : null;
                    ImageClass curImageClass = redoItem.listObjects.Count > 1 ? redoItem.listObjects[1] as ImageClass : null;

                    if(curShape != null)
                        drawingSurface.Children.Remove(curShape);
                    if(curImageClass != null)
                    {
                        SelectedImageBox.ListImageClass.Remove(curImageClass);
                        if (curImageClass.DrawLabel != null)
                            drawingSurface.Children.Remove(curImageClass.DrawLabel);
                    }

                    int arrIndex = redoItem.listObjects.Count > 2? (int)redoItem.listObjects[2] : - 1;
                    string[] arrSeleImportData = redoItem.listObjects.Count > 3? ((IEnumerable)redoItem.listObjects[3]).Cast<object>().Select(x => x.ToString()).ToArray() : null;
                    if (arrSeleImportData != null)
                    {
                        string strAlias = Regex.Match(Regex.Replace(arrSeleImportData[3], @"[""{}]", ""), @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                        var tempImportData = ListDatasheetImportData.FirstOrDefault(temp => temp.DatasheetName == curImageClass.ImportDatasheetName);
                        if (tempImportData != null)
                        {
                            tempImportData.ListImportData.Remove(arrSeleImportData);
                        }

                        char strImageType = SelectedImageBox.ImageBoxName.Contains(settings.SinglePhase) ? 'S' : SelectedImageBox.ImageBoxName.Contains(settings.PhaseContrast) ? 'P' : ' ';
                        var curClassStat = ListClassFolderStat.FirstOrDefault(item => item.ImportDatasheetName == curImageClass.ImportDatasheetName && item.ClassAliasName.ToUpper() == strAlias);
                        if (curClassStat != null)
                        {
                            curClassStat.ClassCount--;
                            if (strImageType == 'S')
                                curClassStat.SingleSpotCount--;
                            else if (strImageType == 'P')
                                curClassStat.PhaseContrastCount--;
                        }
                    }

                    UndoRedoItem undoItem = new UndoRedoItem();
                    undoItem.Type = "Del";
                    undoItem.listObjects.Add(curShape);
                    undoItem.listObjects.Add(curImageClass);
                    if (arrSeleImportData != null){
                        undoItem.listObjects.Add(arrIndex);
                        undoItem.listObjects.Add(arrSeleImportData);
                    }

                    undoRedo.InsertUndoStack(undoItem);
                    IsEnableClassStackPanel = false;
                    UndoButton.IsEnabled = true;
                    bIsSaved = false;
                }

                if (undoRedo.CanRedo())
                    RedoButton.IsEnabled = true;
                else
                    RedoButton.IsEnabled = false;
            }
        }

        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShowMessageNoProject(sender))
                    return;

                if (settings.classCount == 0)
                {
                    System.Windows.MessageBox.Show("Please select proper Class File path from settings tool before import.", "File not found", MessageBoxButton.OK,
                        MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                OpenFileDialog fileOpenDiag = new OpenFileDialog();
                fileOpenDiag.InitialDirectory = settings.LoadCSVImportPath;
                fileOpenDiag.Filter = "csv file|*.csv";
                fileOpenDiag.Multiselect = false;
                DialogResult result = fileOpenDiag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {

                    settings.ImportFilePath = new string[] { fileOpenDiag.FileName };
                    settings.LoadCSVImportPath = System.IO.Path.GetDirectoryName(fileOpenDiag.FileName);
                    settings.LoadImagePath = settings.LoadCSVImportPath;
                    if (settings.CheckFileAccess(settings.ImportFilePath[0]))
                    {
                        System.Windows.MessageBox.Show("File cannot be accessible\nMake sure the file is not accessed by other application.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning,
                            MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }
                    if (ProcessedImageBox.Count > 0)
                        ProcessedImageBox.Clear();
                    //for (int cnt = 0; cnt < ImageMenuList.Count; cnt++)
                    //{
                    //    ImageMenu curImagemenu = ImageMenuList[cnt] as ImageMenu;
                    //    curImagemenu.MenuItemBrush = ImageMenuBrushes[0];
                    //}
                    //LoadProcessedImageFromCSV();
                }
                else
                    return;

                bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += bgwDowork_LoadSegregatedDatasheet;
                bgWorker.ProgressChanged += bgwProgressChange_Load;
                bgWorker.RunWorkerAsync();
                OnWorkerMethodStartWithPercent_ProcessFile(this);
            }

            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show("CSV File import Failed.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("MainWindow::ImportCSV_Click: " + ex.Message, 9);
            }
        }

        private void cmbClassName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string strTemp = cmbClassName.SelectedItem.ToString();
            txtClassID.Text = settings.dictEVSupervisorClass.ToList().Find(s => s.Value == strTemp).Key.ToString();
        }

        /// <summary>
        /// Function to trigger when classname dropdown list item was selected
        /// </summary>
        private void cmbClassName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (cmbClassName.SelectedItem != null)
                {
                    flagClassOp = btnAddClass.Content.ToString() == "Add Class" ? 'A' : 'E';
                    AddClasstoListView();
                }
                else
                    cmbClassName.Focus();
            }
        }

        /// <summary>
        /// Function to reset the image window to fit the image to original size
        /// </summary>
        public void ResetImageWindow(UIElement Element, Char ch = ' ')
        {
            if (Element != null)
            {
                var st = GetScaleTransform(Element);
                var tt = GetTranslateTransform(Element);
                ImageZoomBorder zoomBorder = border as ImageZoomBorder;

                double defaultScale = 0;
                //if (Element.DependencyObjectType.Name == "Canvas")
                //    defaultScale = (((Element as Canvas).Parent as FrameworkElement).ActualHeight / (Element as Canvas).Height) - 0.05;
                //else if (Element.DependencyObjectType.Name == "Image")
                //    defaultScale = (((Element as Image).Parent as FrameworkElement).ActualHeight / (Element as Grid).Height) - 0.05;

                if(bWorkCellMode)
                    defaultScale = (((Element as FrameworkElement).Parent as FrameworkElement).ActualHeight / (Element as FrameworkElement).Height) - 0.005;
                else 
                    defaultScale = (((Element as FrameworkElement).Parent as FrameworkElement).ActualHeight / (Element as FrameworkElement).Height) - 0.05;

                if (ch == 'F' && st.ScaleX != defaultScale)
                {
                    //reset to original size
                    st.ScaleX = defaultScale;
                    st.ScaleY = defaultScale;

                    tt.X = 1;
                    tt.Y = 1;
                    zoomBorder.zoomX = 0;
                    lblZoomStatus.Content = "Fit to Original size";
                    lblZoomStatus.Visibility = Visibility.Visible;
                    System.Windows.Media.Animation.Storyboard sb = this.Resources["sbHideZoomLabel"] as System.Windows.Media.Animation.Storyboard;
                    sb.Begin(lblZoomStatus);
                }
                else if (ch != 'F')
                {
                    //loads to default set zoom level
                    //settings.ZoomLevel = 0.1;
                    if(!bWorkCellMode)
                    {
                        double zoom = settings.ZoomLevel * 0.6;
                        st.ScaleX = defaultScale + zoom;
                        st.ScaleY = defaultScale + zoom;
                    }
                    else
                    {
                        st.ScaleX = defaultScale;
                        st.ScaleY = defaultScale;
                    }

                    tt.X = 1;
                    tt.Y = 1;
                    zoomBorder.zoomX = settings.ZoomLevel;
                }
            }
        }

        private void lvClass_ContextEdit(object sender, MouseButtonEventArgs e)
        {
            if (lvImageClass.SelectedIndex == -1)
                return;
            IsEnableClassStackPanel = true;
            btnAddClass.Content = "Update Class";
            btnAddClass.ToolTip = "Update Class";
            lvImageClass.Focus();
            this.cmbClassName.Focus();
            cmbClassName.IsDropDownOpen = true;
        }

        /// <summary>
        /// Function to trigger the event when class menu drop down list closed. It adds the class and shape to selected images
        /// </summary>
        private void cmbClassName_DropDownClosed(object sender, EventArgs e)
        {
            if(cmbClassName.SelectedItem == null && settings.ClassType == EnumClassType.Segregation)
            {
                txtClassID.Text = "";
                cmbClassName.SelectionChanged -= cmbClassName_SelectionChanged;
                cmbClassName.Text = "";
                cmbClassName.SelectionChanged += cmbClassName_SelectionChanged;
                IsEnableClassStackPanel = false;
                return;
            }
            else if (cmbClassName.SelectedItem != null)
                AddClasstoListView();
        }

        private void SideListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
                e.Handled = true;
        }

        /// <summary>
        /// Function to Add the classname to drawn shape when Add class contextmenu selected
        /// </summary>
        private void AddImageAttributeToClass_Click(object sender, MouseButtonEventArgs e)
        {
            if(settings.ClassType == EnumClassType.Segregation)
            {
                flagClassOp = 'S';
                IsEnableClassStackPanel = true;
                this.cmbClassName.IsDropDownOpen = true;
                this.cmbClassName.Focus();
                return;
            }
            else if (!bIsClassAdded && selShape != null)
            {
                flagClassOp = 'A';
                IsEnableClassStackPanel = true;
                this.cmbClassName.IsDropDownOpen = true;
                this.cmbClassName.Focus();
            }
        }

        private void CopyPasteImageAttribute_Click(object sender, MouseButtonEventArgs e)
        {
            if (resizeShape != null && ResizeImageClass != null)
            {
                flagClassOp = 'C';
                AddClasstoListView();
            }
        }

        /// <summary>
        /// Function to Copy the drawn shape from one image to another when Copy contextmenu selected
        /// </summary>
        private void CopyImageAttribute_Click(object sender, MouseButtonEventArgs e)
        {
            if (resizeShape != null && ResizeImageClass != null)
            {
                copyShape = resizeShape;
                bIsClassAdded = true;
            }
        }

        /// <summary>
        /// Function to Paste the copied shape from one image to another when Paste contextmenu selected
        /// </summary>
        private void PasteImageAttribute_Click(object sender, MouseButtonEventArgs e)
        {
            if (bIsClassAdded && copyShape != null)
            {
                PointCollection getPolyPoints = new PointCollection();
                if (copyShape.DependencyObjectType.Name == "Rectangle")
                {
                    Rectangle selRect = new Rectangle();
                    selShape = selRect;
                }
                else if (copyShape.DependencyObjectType.Name == "Ellipse")
                {
                    Ellipse selCircle = new Ellipse();
                    selShape = selCircle;
                }
                else if (copyShape.DependencyObjectType.Name == "Polyline")
                {
                    Polyline selPolyline = new Polyline();
                    getPolyPoints = new PointCollection();
                    selPolyline.Points = getPolyPoints;
                    selShape = selPolyline;
                }
                selShape.Stroke = Brushes.Blue;
                selShape.StrokeThickness = ShapeStrokeThickness;
                selShape.StrokeLineJoin = PenLineJoin.Round;
                selShape.StrokeStartLineCap = PenLineCap.Round;
                selShape.StrokeEndLineCap = PenLineCap.Round;
                selShape.Fill = Brushes.Transparent;

                if (selShape.DependencyObjectType.Name == "Rectangle" || selShape.DependencyObjectType.Name == "Ellipse")
                {
                    Canvas.SetLeft(selShape, PasteMouseLocation.X);
                    Canvas.SetTop(selShape, PasteMouseLocation.Y);
                    selShape.Width = copyShape.Width;
                    selShape.Height = copyShape.Height;
                }
                else if (selShape.DependencyObjectType.Name == "Polyline")
                {
                    Polyline curPolyline = copyShape as Polyline;
                    double offset_x = PasteMouseLocation.X - curPolyline.Points[0].X;
                    double offset_y = PasteMouseLocation.Y - curPolyline.Points[0].Y;
                    for (int index = 0; index < curPolyline.Points.Count; index++)
                    {
                        getPolyPoints.Add(new Point(curPolyline.Points[index].X + offset_x, curPolyline.Points[index].Y + offset_y));
                    }
                }

                drawingSurface.ClipToBounds = true;
                drawingSurface.Children.Add(selShape);
                bIsClassAdded = false;
                resizeShape = selShape;
                if (selShape.Width > 0 && selShape.Height > 0)
                {
                    if (selShape.DependencyObjectType.Name == "Ellipse")
                        StatusSelectedDimension = Convert.ToInt32(selShape.Width) + " x " + Convert.ToInt32(selShape.Height) + "    rad : " + (int)(selShape.Width / 2);
                    else
                        StatusSelectedDimension = Convert.ToInt32(selShape.Width) + " x " + Convert.ToInt32(selShape.Height);
                }
                else
                    StatusSelectedDimension = "";
            }
        }

        /// <summary>
        /// Function to Save drawn shape to external drive when Save to disk contextmenu selected
        /// </summary>
        private void SaveROItoDisk_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (resizeShape != null && ResizeImageClass != null)
                {
                    ImageMenu currentImage = listBoxImages.SelectedItem as ImageMenu;
                    BitmapImage croppedImage = new BitmapImage();
                    using (FileStream stream = Delimon.Win32.IO.File.OpenRead(currentImage.ImagePath))
                    {
                        croppedImage.BeginInit();
                        croppedImage.CacheOption = BitmapCacheOption.OnLoad;
                        croppedImage.StreamSource = stream;
                        croppedImage.EndInit();
                    }

                    int x = 0, y = 0, width = 0, height = 0;
                    if (ResizeImageClass.Shape == EnumSelectedShape.Rectangle || ResizeImageClass.Shape == EnumSelectedShape.Circle)
                    {
                        x = Convert.ToInt32(ResizeImageClass.XCoordinate);
                        y = Convert.ToInt32(ResizeImageClass.YCoordinate);
                        width = Convert.ToInt32(ResizeImageClass.Width);
                        height = Convert.ToInt32(ResizeImageClass.Height);
                    }

                    else if (ResizeImageClass.Shape == EnumSelectedShape.Polyline)
                    {
                        x = Convert.ToInt32(ResizeImageClass.All_Points_X.Min());
                        y = Convert.ToInt32(ResizeImageClass.All_Points_Y.Min());
                        int x1 = Convert.ToInt32(ResizeImageClass.All_Points_X.Max());
                        int y1 = Convert.ToInt32(ResizeImageClass.All_Points_Y.Max());
                        width = x1 - x;
                        height = y1 - y;
                    }

                    CroppedBitmap CroppedBitmap = new CroppedBitmap(croppedImage, new Int32Rect(x, y, width, height));

                    string strDataPath = settings.CSVExportPath + @"\Output Data\Cropped Images\";
                    if (!Directory.Exists(strDataPath))
                        Directory.CreateDirectory(strDataPath);

                    string strCropImageName = SelectedImageBox.ImageBoxName.Replace(System.IO.Path.GetExtension(SelectedImageBox.ImageBoxName), string.Empty) + "_" + ResizeImageClass.ClassName;
                    string strCSVSavePath = System.IO.Path.Combine(strDataPath, strCropImageName + ".bmp");
                    PngBitmapEncoder pngImage = new PngBitmapEncoder();
                    pngImage.Frames.Add(BitmapFrame.Create(CroppedBitmap));
                    while (File.Exists(strCSVSavePath))
                    {
                        string strReplace = strCSVSavePath.Replace(".bmp", string.Empty);
                        string temp = strReplace.Substring(strReplace.Length - 2, 2);
                        int n;
                        bool bIsIntCheck = Int32.TryParse(temp.Substring(1, 1), out n);
                        strCSVSavePath = System.IO.Path.Combine(strDataPath, strCropImageName + "_" + (n + 1).ToString() + ".bmp");
                    }
                    using (Stream fileStream = File.Create(strCSVSavePath))
                    {
                        pngImage.Save(fileStream);
                    }

                    System.Windows.MessageBox.Show("ROI image saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                }
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                System.Windows.MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("MainWindow::SaveROItoDisk_Click: " + ex.Message, 9);
            }
        }

        private void ShowBoundingBox_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (resizeShape != null && ResizeImageClass != null && ResizeImageClass.Shape == EnumSelectedShape.Polyline)
                {
                    double x = ResizeImageClass.All_Points_X.Min();
                    double y = ResizeImageClass.All_Points_Y.Min();
                    double x1 = ResizeImageClass.All_Points_X.Max();
                    double y1 = ResizeImageClass.All_Points_Y.Max();
                    double width = x1 - x;
                    double height = y1 - y;

                    if (selBoundBox != null)
                        return;

                    Shape boundShape = new Rectangle();
                    boundShape.Width = width;
                    boundShape.Height = height;
                    Canvas.SetLeft(boundShape, x);
                    Canvas.SetTop(boundShape, y);

                    boundShape.Stroke = Brushes.Yellow;
                    boundShape.StrokeThickness = 2;
                    boundShape.StrokeLineJoin = PenLineJoin.Round;
                    boundShape.StrokeStartLineCap = PenLineCap.Round;
                    boundShape.StrokeEndLineCap = PenLineCap.Round;
                    boundShape.Fill = Brushes.Transparent;

                    drawingSurface.Children.Add(boundShape);
                    selBoundBox = boundShape;
                }
            }

            catch (Exception ex)
            {
                Utilities.LogMessage("MainWindow::SaveROItoDisk_Click: " + ex.Message, 9);
            }
        }

        /// <summary>
        /// Function to Load Image folder when Load Images menu selected
        /// It also enables the checklabelling thread
        /// </summary>
        private void LoadImageFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = settings.DefaultImageLoadPath;
            DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ResetWindow();
                settings.LoadImagePath = folderDialog.SelectedPath;
                settings.DefaultImageLoadPath = folderDialog.SelectedPath;
                labelEvent.Reset();                                             //Stop the CheckLabelling thread
                SaveEvent.Reset();                                              //Stop the AutoSaveWork thread
                this.Dispatcher.Invoke((() =>
                {
                    ImageMenuList.Clear();
                }));
                ImageAnalysisList.Clear();
                ListAnalysisModule.Clear();
                ResetAnalyisImageWindow();
                SPSorting.Visibility = Visibility.Collapsed;
                SPSearch.Visibility = Visibility.Collapsed;
                StatusNoteVisiblity = Visibility.Collapsed;
                txtSearchText.TextChanged -= txtSearchText_TextChanged;
                txtSearchText.Clear();
                txtSearchText.TextChanged += txtSearchText_TextChanged;
                listBoxImages.ItemsSource = null;
                listAnalysisImages.ItemsSource = null;

                bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += bgwDowork_LoadImages;
                bgWorker.ProgressChanged += bgwProgressChange_Load;
                bgWorker.RunWorkerAsync();
                OnWorkerMethodStart();

                threadCheckLabelling = new Thread(CheckLabellingThread);
                threadCheckLabelling.IsBackground = true;
                threadCheckLabelling.Start();
                threadCheckLabelling.Priority = ThreadPriority.Lowest;
                settings.CSVExportPath = settings.LoadImagePath;
                settings.LoadCSVImportPath = folderDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Function to Import CSV files when Import CSV menu selected
        /// </summary>
        private void ImportMultipleCSV_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            AddMultipleImportFile windowSettings = new AddMultipleImportFile(this, "CSV");
            windowSettings.Owner = this;
            windowSettings.spSuggestion.Visibility = settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            windowSettings.ShowDialog();
        }

        /// <summary>
        /// Function to filter the All images, lablelled, correction images and non labelled images
        /// </summary>
        public void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSort.SelectedIndex == -1)
                return;
            ResetWindow();

            cmbClassFilter.Visibility = cmbSort.SelectedIndex == 1 ? Visibility.Collapsed : Visibility.Visible;
            cmbClassFilter.SelectedIndex = 0;
            listBoxImages.ItemsSource = cmbSort.SelectedIndex > 0 ? ImageMenuList.Where(item => item.MenuItemBrush == ImageMenuBrushes[cmbSort.SelectedIndex - 1]) : ImageMenuList;
            listBoxImages.SelectedIndex = -1;
            txtSearchText.TextChanged -= txtSearchText_TextChanged;
            txtSearchText.Clear();
            txtSearchText.TextChanged += txtSearchText_TextChanged;
        }

        /// <summary>
        /// Function to Export JSON file to output folder when Export JSON menu selected
        /// </summary>
        private void JSONExport_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            bool bIsExportSavedFile = false;
            bool bIsExportRawFile = false;
            string strDataPath = settings.StatsFilePath + @"\Temp Files\Format File";
            string[] tempFiles = null;
            bool bIsCocoJsonExport = (sender as Telerik.Windows.Controls.RadMenuItem).Header.ToString() == "COCO Compatible" ? true : false;

            if (System.IO.Directory.Exists(strDataPath))
                tempFiles = Directory.GetFiles(strDataPath, "*.json");

            bool bIsFormatFile = tempFiles != null && tempFiles.Length > 0 ? true : false;
            if (!bIsFormatFile && (ImageMenuList == null || ImageMenuList.Count == 0)){
                System.Windows.MessageBox.Show("Nothing to export..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (bIsFormatFile && (ImageMenuList == null || ImageMenuList.Count == 0)){
                //MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export Saved Format file into Output Folder?", "Export Format File", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (result == MessageBoxResult.No)
                //    return;
                //else
                //{
                bIsExportSavedFile = true;
                bIsExportRawFile = false;
                //}
            }
            else if (!bIsFormatFile && ImageMenuList.Count > 0){
                //string filtype = bIsCocoJsonExport ? "COCO compatible JSON" : "Raw JSON";
                //MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export as " + filtype + " file into Output Folder?", "Export Raw file", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (result == MessageBoxResult.No)
                //    return;
                //else{
                bIsExportSavedFile = false;
                bIsExportRawFile = true;
                //}
            }
            else if (ImageMenuList.Count > 0 && bIsFormatFile){
                MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export Saved Format file along with Raw JSON into Output Folder?", "Export Raw File", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                           MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);

                if (result == MessageBoxResult.No){
                    bIsExportSavedFile = true;
                    bIsExportRawFile = false;
                }
                else{
                    bIsExportSavedFile = true;
                    bIsExportRawFile = true;
                }
            }

            object[] args = { bIsExportSavedFile, bIsExportRawFile, bIsCocoJsonExport };
            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_JSONFileExport;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(args);
            OnWorkerMethodStart_withPercentage();
        }

        /// <summary>
        /// Function to Export All labelled Region of Image into external output folder when Export All ROI menu selected
        /// </summary>
        private void AllROIExport_Click(object sender, RoutedEventArgs e)
        {
            if (TotalMultiClassLabelled <= 0)
            {
                System.Windows.MessageBox.Show("Labelled images not found..!", "No ROI", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            ClassSelectionWindow selectionWindow = new ClassSelectionWindow(this, "ROI");
            selectionWindow.Owner = this;
            selectionWindow.ShowDialog();
        }

        private void RibbonLoaded(object sender, RoutedEventArgs e)
        {
            Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                //child.RowDefinitions[0].Height = new GridLength(0);
                //((System.Windows.Controls.MenuItem)RibbonWin.ContextMenu.Items[0]).Visibility = Visibility.Collapsed;
            }
        }

        public List<string> ListFilteredClass = new List<string>();
        /// <summary>
        /// Function to filter the labelled images w.r.t their class index selected using class combo box
        /// </summary>
        public void cmbClassFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSort.SelectedIndex == -1 || cmbClassFilter.SelectedIndex == -1)
                return;
            ResetWindow();

            List<string> listTemp = new List<string>();
            foreach (string item in cmbClassFilter.SelectedItems)
            {
                listTemp.Add(item);
            }

            if (listTemp.Count > 0 && listTemp.Last() == "All")
            {
                cmbClassFilter.SelectedIndex = 0;
                cmbClassFilter.IsDropDownOpen = false;
            }
            else if (listTemp.Count > 0)
            {
                if (listTemp.Contains("All"))
                {
                    cmbClassFilter.SelectedItems.Remove("All");
                    listTemp.Remove("All");
                }
            }
            ListFilteredClass = new List<string>(listTemp);

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_cmbClassFilter_SelectionChanged;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStart();
        }

        /// <summary>
        /// Function to filter the labelled images w.r.t their class index selected using class combo box in side image menu list 
        /// </summary>
        private void LoadClassFilteredImages()
        {
            int nIndexSort = 0;
            int nIndexClassFilter = 0;
            string strClassFilter = "";
            this.Dispatcher.Invoke(() =>
            {
                nIndexSort = cmbSort.SelectedIndex;
                nIndexClassFilter = cmbClassFilter.SelectedIndex;
                strClassFilter = cmbClassFilter.SelectedItem.ToString();
            });
            if (!ListFilteredClass.Contains("All"))
            {
                ImageMenu[] arrImageMenuList = ImageMenuList.AsParallel().Where(item => item.MenuItemBrush != ImageMenuBrushes[0]).ToArray();
                this.Dispatcher.Invoke(() =>
                {
                    listBoxImages.ItemsSource = (nIndexSort == 0) ? arrImageMenuList.Where(item => item.ImageBox.ListImageClass.Select(s => s.ClassAlias).Any(c => ListFilteredClass.Contains(c))) :
                                arrImageMenuList.Where(item => item.MenuItemBrush == ImageMenuBrushes[nIndexSort - 1] && item.ImageBox.ListImageClass.Select(s => s.ClassAlias).Any(c => ListFilteredClass.Contains(c)));
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    listBoxImages.ItemsSource = cmbSort.SelectedIndex == 0 ? ImageMenuList.ToList() :
                                        ImageMenuList.AsParallel().Where(item => item.MenuItemBrush == ImageMenuBrushes[nIndexSort - 1]).ToList();
                });
            }

            this.Dispatcher.Invoke(() =>
            {
                listBoxImages.SelectedIndex = -1;
                listBoxImages.SelectedIndex = listBoxImages.Items.Count > 0 ? 0 : -1;
                txtSearchText.TextChanged -= txtSearchText_TextChanged;
                txtSearchText.Clear();
                txtSearchText.TextChanged += txtSearchText_TextChanged;
                OnWorkerMethodComplete("Complete");
            });
        }

        /// <summary>
        /// Function to check Duplicate image statistics when Duplicate image stat menu selected 
        /// </summary>
        private void DuplicateImageStats_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            DuplicateImageStats duplicateStatWindow = new DuplicateImageStats(this);
            duplicateStatWindow.Owner = this;
            duplicateStatWindow.ShowDialog();
        }

        /// <summary>
        /// Function to Import Json files when Import JSON menu selected 
        /// </summary>
        private void ImportMultipleJSON_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            AddMultipleImportFile windowSettings = new AddMultipleImportFile(this, "JSON");
            windowSettings.spSuggestion.Visibility = settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            windowSettings.Owner = this;
            windowSettings.ShowDialog();
        }

        /// <summary>
        /// Function to get Validation report for CSV file when Validate CSV menu selected  
        /// </summary>
        private void ValidationCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShowMessageNoProject(sender))
                    return;

                if (!settings.blnValidationStat)
                {
                    System.Windows.MessageBox.Show("Please Enable Validation Stats in \"File->Settings\" menu to Open CSV Validation Report..", "Access Denied",
                        MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    Utilities.LogMessage("Enable Validation Stats in settings.");
                    return;
                }

                if (!CheckCSVFileLoaded())
                    return;

                for (int i = 0; i < settings.ImportFilePath.Length; i++)
                {
                    if (settings.CheckFileAccess(settings.ImportFilePath[i]))
                    {
                        System.Windows.MessageBox.Show("Some CSV files cannot be accessible\nMake sure the file is not accessed by other application.", "Access Denied", MessageBoxButton.OK,
                            MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }
                }
                ValidationReportWindow windowValidation = new ValidationReportWindow(this);
                windowValidation.Owner = this;
                //if (windowValidation.bLoadSuccess)
                windowValidation.ShowDialog();
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Something wend wrong.. Could not Validate", "Error", MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Validation of CSV File : " + ex.Message, 0);
            }
        }

        /// <summary>
        /// Function to open FormatDatasheet tool for CSV/JSON Files 
        /// </summary>
        private void FormatCSVDatasheet_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            string fileType = "";
            if (settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 && settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".csv"))
                fileType = "CSV";
            else if(settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 && settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".json"))
                fileType = "JSON";

            if (fileType == "")
            {
                //System.Windows.MessageBox.Show("CSV/JSON File not found..!\nPlease Import from File Menu->Import CSV/JSON file.", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                //                                MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                System.Windows.MessageBox.Show("CSV File not found..!\nPlease Import from File Menu->Import CSV file.", "File not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                                MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage(fileType + " file not found.");
                return;
            }

            FormatDataSheet windowFormat = new FormatDataSheet(this, fileType);
            windowFormat.Owner = this;
            windowFormat.ShowDialog();
        }

        /// <summary>
        /// Function to open FormatDatasheet tool for JSON Files 
        /// </summary>
        private void FormatJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (!CheckCSVFileLoaded(".json"))
                return;

            FormatDataSheet windowFormat = new FormatDataSheet(this, "JSON");
            windowFormat.Owner = this;
            windowFormat.ShowDialog();
        }

        /// <summary>
        /// Function to open Image validation report to check CSV files images and loaded images list 
        /// </summary>
        private void ImageValidation_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (!CheckCSVFileLoaded())
                return;

            ImageValidationWindow windowValidate = new ImageValidationWindow(this);
            windowValidate.Owner = this;
            windowValidate.ShowDialog();
        }

        /// <summary>
        /// Function to do filteration w.r.t the image name entered 
        /// </summary>
        private void txtSearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ImageMenuList.Count == 0)
                return;
            ResetWindow();
            cmbClassFilter.SelectionChanged -= cmbClassFilter_SelectionChanged;
            cmbSort.SelectionChanged -= cmbSort_SelectionChanged;
            cmbClassFilter.SelectedIndex = 0;
            cmbSort.SelectedIndex = 0;
            listBoxImages.ItemsSource = ImageMenuList.Where(item => item.ImageName.ToLower().Contains(txtSearchText.Text.Trim().ToLower()));
            cmbClassFilter.SelectionChanged += cmbClassFilter_SelectionChanged;
            cmbSort.SelectionChanged += cmbSort_SelectionChanged;
        }

        /// <summary>
        /// Function to do Reset entire application by clearing all stats, files, images loaded 
        /// </summary>
        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Do you wish to Reset Application?", "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                ResetApplication(true);
            }
        }

        public void ResetApplication(bool bIsManual = false)
        {
            labelEvent.Reset();
            SaveEvent.Reset();
            if (ImageMenuList != null)
                ImageMenuList.Clear();

            if (ImageAnalysisList != null)
                ImageAnalysisList.Clear();
            if(ListAnalysisModule != null)
                ListAnalysisModule.Clear();

            listBoxImages.ItemsSource = null;
            listAnalysisImages.ItemsSource = null;
            TotalImagesPresent = 0;
            TotalImagesLoaded = 0;
            TotalDuplicateImages = 0;
            CleanupLoadedData(true);
            settings.ImportFilePath = null;
            settings.nImportFileRecordCount = null;
            ResetWindow();
            SPSorting.Visibility = Visibility.Collapsed;
            SPSearch.Visibility = Visibility.Collapsed;
            ResetAnalyisImageWindow();
            ClearGraphs();
            ListDataAugmentTypeClass.Clear();

            if (bIsManual)
            {
                dtAutoSaveTime = DateTime.Now;
                LastSavedWorkTime = "";
                lvStatistics.ItemsSource = null;
                lvAutoPilotBVStat.ItemsSource = null;
                lvAutoPilotIPIeStat.ItemsSource = null;
                ChartNonStackClass.Series.Clear();
                ChartStackedClass.Series.Clear();
                btnStackChart.Visibility = Visibility.Collapsed;
                AugmentExportCount = 0;
                AutoPilotNonProcImages = 0;
                AutoPilotTotalImages = 0;
                AutoPilotProcessedImages = 0;
                lblAugmentStatus.Content = "";
                lblAutoPilotStatus.Content = "";
                lblImgAnalyzeStatus.Content = "";
                dtLastAugmentTime = new DateTime();
                dtLastAutoPilotTime = new DateTime();
                dtLastImgAnalysedTime = new DateTime();
                UndoButton.IsEnabled = false;
                RedoButton.IsEnabled = false;                

                string Workdir = settings.StatsFilePath + @"\GenieSupervisor_WorkStats";
                if(Directory.Exists(Workdir))
                {
                    string[] StatsFile = Directory.GetFiles(Workdir, "*AugmentStat*.bin");
                    if (StatsFile.Length > 0)
                    {
                        foreach (string file in StatsFile)
                            File.Delete(file);
                    }

                    StatsFile = Directory.GetFiles(Workdir, "*AutoPilotStat*.bin");
                    if (StatsFile.Length > 0)
                    {
                        foreach (string file in StatsFile)
                            File.Delete(file);
                    }
                }
               
                SetAnalysisDisplayButton(false);
                SetAnalysisProcessButton(true);
            }
            Utilities.LogMessage("Application got reset successfully.");
        }
        /// <summary>
        /// Function to open Class folder stats report to find the count of class loaded from import data 
        /// </summary>
        private void ClassStatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            ClassFolderStats windowFolderStat = new ClassFolderStats(this);
            windowFolderStat.Owner = this;
            windowFolderStat.ShowDialog();
        }

        /// <summary>
        /// Function to open Datasheet splitter tool to split and shuffle import CSV files with images in pair 
        /// </summary>
        private void DatasheetSplit_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (!CheckCSVFileLoaded())
                return;

            DatasheetSplitterWindow windowDatasheetSplit = new DatasheetSplitterWindow(this);
            windowDatasheetSplit.Owner = this;
            windowDatasheetSplit.ShowDialog();
        }

        /// <summary>
        /// Function to Export XML files to Output folder 
        /// </summary>
        private void ExportXMLButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ProcessedImageBox.Count == 0)
            {
                System.Windows.MessageBox.Show("Could not found CSV file or annotations..!\nPlease got to File Menu->Import CSV file.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            FormatXMLWindow windowFormatXML = new FormatXMLWindow(this);
            windowFormatXML.Owner = this;
            windowFormatXML.ShowDialog();
        }

        /// <summary>
        /// Function to Open ID Assigner window to edit Class ID to csv files and export the same 
        /// </summary>
        private void IDAssignerButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (!CheckCSVFileLoaded())
                return;

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            ClassIDAssignerWindow windowIDAssigner = new ClassIDAssignerWindow(this);
            windowIDAssigner.Owner = this;
            Mouse.OverrideCursor = null;
            windowIDAssigner.ShowDialog();
        }

        /// <summary>
        /// Function to import XML files by folder selecting Import XML menu 
        /// </summary>
        private void ImportXMLButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            AddMultipleImportFile windowAddFiles = new AddMultipleImportFile(this, "XML");
            windowAddFiles.spSuggestion.Visibility = settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            windowAddFiles.Owner = this;
            windowAddFiles.ShowDialog();
        }

        private void QuickPallete_Expanded(object sender, RoutedEventArgs e)
        {
            if ((sender as Expander).IsExpanded == true)
                (sender as Expander).ToolTip = "Hide Quick Palette";
            else
                (sender as Expander).ToolTip = "Show Quick Palette";
        }

        private void ImportTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            OpenFileDialog openFileDiag = new OpenFileDialog();
            openFileDiag.Filter = "text file|*.txt";
            DialogResult result = openFileDiag.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                settings.ImportFilePath = openFileDiag.FileNames;

                bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += bgwDowork_LoadPredictionText;
                bgWorker.RunWorkerAsync();
                OnWorkerMethodStartWithPercent_ProcessFile(this);
            }
        }

        private void ManageDatasheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (settings.ImportFilePath == null || settings.ImportFilePath.Length == 0)
            {
                System.Windows.MessageBox.Show("No Datasheet has been found to manage..!\nPlease Import Datasheet from menu.", "Datasheet not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;

            }

            ManageAndMergeDatasheet windowManageDatasheet = new ManageAndMergeDatasheet(this);
            windowManageDatasheet.Owner = this;
            windowManageDatasheet.ShowDialog();
        }

        private void tabSideBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.Source is System.Windows.Controls.TabControl))
                return;

            if(tabSideBar.SelectedIndex == 0 || tabSideBar.SelectedIndex == 1)
            {
                gridSideBar.SetValue(Grid.ColumnProperty, 0);
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 1);
                gridImageAttribute.Visibility = Visibility.Visible;
                gridStats.Visibility = Visibility.Visible;
                gridDrawing.Visibility = Visibility.Visible;
                gSplitHorizontal.Visibility = Visibility.Visible;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
            }
            else if (tabSideBar.SelectedIndex == 2)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;

                ResetWindow();
                listBoxImages.SelectedItem = null;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Visible;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                if (ListClassFolderStat.Count > 0) {
                    RefreshAugmentationClassList();
                }
            }
            else if(tabSideBar.SelectedIndex == 3)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Visible;
                ResetWindow();
                listBoxImages.SelectedItem = null;
                NotifyPropertyChanged("ChartDisplayName");
                if (ImageMenuList != null && ImageMenuList.Count > 0)
                    lblAutoPilotStatus.Content = dtLastAutoPilotTime == new DateTime() ? "Last Auto Pilot : Never" : "Last Auto Pilot : " + dtLastAutoPilotTime.ToShortDateString() + " " + dtLastAutoPilotTime.ToShortTimeString();
            }
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

        private void txtTarget_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            AugmentTypeClass curClass = (sender as System.Windows.Controls.TextBox).DataContext as AugmentTypeClass;
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if ((sender as System.Windows.Controls.TextBox).Text == "")
                    (sender as System.Windows.Controls.TextBox).Text = "0";
            }

            else if ((e.Key == Key.Enter || e.Key == Key.Down))
            {
                int index = ListAugmentTypeClass.IndexOf(curClass);
                if (index < ListAugmentationView.Items.Count)
                {
                    ListAugmentationView.SelectedIndex = index + 1;
                    AugmentTypeClass nextClassStat = ListAugmentTypeClass[ListAugmentationView.SelectedIndex];
                    if (nextClassStat.IsTypeEnable)
                        SetTextBoxFocus(nextClassStat);
                }
            }
            else if (e.Key == Key.Up)
            {
                int index = ListAugmentTypeClass.IndexOf(curClass);
                if (index > 0)
                {
                    ListAugmentationView.SelectedIndex = index - 1;
                    AugmentTypeClass prevClassStat = ListAugmentTypeClass[ListAugmentationView.SelectedIndex];
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

        private void tabSideBar_SelectionChanged(object sender, Telerik.Windows.Controls.RadSelectionChangedEventArgs e)
        {
            if (!(e.Source is Telerik.Windows.Controls.RadTabControl))
                return;

            sbSideBarButton.Visibility = Visibility.Collapsed;
            if (tabSideBar.SelectedIndex == 0 || tabSideBar.SelectedIndex == 1)
            {
                gridSideBar.SetValue(Grid.ColumnProperty, 0);
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 1);
                gridImageAttribute.Visibility = Visibility.Visible;
                gridStats.Visibility = Visibility.Visible;
                gridDrawing.Visibility = Visibility.Visible;
                gSplitHorizontal.Visibility = Visibility.Visible;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Collapsed;
                sbSideBarButton.Visibility = Visibility.Visible;
            }
            else if (tabSideBar.SelectedIndex == 2)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;

                ResetWindow();
                listBoxImages.SelectedItem = null;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Visible;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Collapsed;
                if (ListClassFolderStat.Count > 0)
                {
                    RefreshAugmentationClassList();
                }
            }
            else if (tabSideBar.SelectedIndex == 3)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Visible;
                ResetWindow();
                listBoxImages.SelectedItem = null;
                NotifyPropertyChanged("ChartDisplayName");
                if (ImageMenuList != null && ImageMenuList.Count > 0)
                    lblAutoPilotStatus.Content = dtLastAutoPilotTime == new DateTime() ? "Last Auto Pilot : Never" : "Last Auto Pilot : " + dtLastAutoPilotTime.ToShortDateString() + " " + dtLastAutoPilotTime.ToShortTimeString();
            }
            else if (tabSideBar.SelectedIndex == 4)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Visible;
                ResetWindow();
                listBoxImages.SelectedItem = null;
                if (ImageMenuList != null && ImageMenuList.Count > 0 && dtLastImgAnalysedTime == new DateTime())
                    lblImgAnalyzeStatus.Content = "Last Image Analysed : Never";
            }
            else if (tabSideBar.SelectedIndex == 5)
            {
                gridSideBar.SetValue(Grid.ColumnSpanProperty, 3);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gridStats.Visibility = Visibility.Collapsed;
                gridDrawing.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;
                StatusNoteVisiblity = Visibility.Collapsed;
                lblAugmentStatus.Visibility = Visibility.Collapsed;
                lblAutoPilotStatus.Visibility = Visibility.Collapsed;
                lblImgAnalyzeStatus.Visibility = Visibility.Collapsed;
                ResetWindow();
                listBoxImages.SelectedItem = null;
                LoadAllVisualizationGraphs();
            }
        }

        private void ManageProjectButton_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            ProjectMenu.IsOpen = false;
            string type = (sender as Telerik.Windows.Controls.RadMenuItem).Header.ToString() == "New Project" ? "New" : "Edit";
            ProjectConfigWindow projectWindow = new ProjectConfigWindow(this, type);
            projectWindow.Owner = this;
            projectWindow.ShowDialog();
            UpdateNotifyPropertyChnages();
        }

        private void ImageFormatChange_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ImageAnalysisList == null || ImageAnalysisList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load images to change Image Format..", "No Images", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            string strImageFormat = "";
            if ((sender as Telerik.Windows.Controls.RadMenuItem).Header.ToString() == "Export BMP to JPEG")
                strImageFormat = ".jpeg";
            else
                strImageFormat = ".bmp";

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_ImageFormatChange;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(strImageFormat);
            OnWorkerMethodStartWithPercent_ProcessFile(this, "Please wait while Processing..");
        }

        /// <summary>
        /// Function to Load last saved work from EVAnnotator folder 
        /// </summary>
        private void LoadLastWork_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            try
            {
                if (ShowMessageNoProject(sender))
                    return;

                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                //string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
                string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";
                if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                    return;
                if (!Directory.Exists(Workdir))
                    Directory.CreateDirectory(Workdir);
                string[] StatsFile = Directory.GetFiles(Workdir, "*Savedata*.bin");
                if (StatsFile.Length == 0)
                {
                    System.Windows.MessageBox.Show("No saved work found to load for selected project..!", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to load last saved work?", "Load Work", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;

                ResetWindow();
                CleanupLoadedData();
                if (ImageMenuList != null)
                    ImageMenuList.Clear();
                bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += bgwDowork_LoadProcessedImageStat;
                bgWorker.ProgressChanged += bgwProgressChange_Load;
                bgWorker.RunWorkerAsync();
                OnWorkerMethodStartWithPercent_ProcessFile(this);

                if (threadCheckLabelling == null)
                {
                    threadCheckLabelling = new Thread(CheckLabellingThread);
                    threadCheckLabelling.IsBackground = true;
                    threadCheckLabelling.Start();
                    threadCheckLabelling.Priority = ThreadPriority.Lowest;
                }
            }

            catch (Exception ex)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Error while loading last saved work..!", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("LoadSavedWorkHistory " + ex.Message, 9);
            }
        }

        /// <summary>
        /// Function to Clear last saved work history 
        /// </summary>
        private void ClearHistory_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
            if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                return;

            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to clear saved work history?", "Clear History", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            //string Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
            string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";
            if (Directory.Exists(Workdir))
            {
                string[] StatsFile = Directory.GetFiles(Workdir, "*.bin");
                if (StatsFile.Count() > 0)
                {
                    foreach (string file in StatsFile)
                    {
                        File.Delete(file);
                    }
                }
            }

            Workdir = settings.StatsFilePath + @"GenieSupervisor_WorkStats";
            if (Directory.Exists(Workdir))
            {
                string[] StatsFile = Directory.GetFiles(Workdir, "*.bin");
                if (StatsFile.Count() > 0)
                {
                    foreach (string file in StatsFile)
                    {
                        File.Delete(file);
                    }
                }
            }
            Utilities.LogMessage("Saved works cleared from path " + Workdir, 0);
        }

        private void RetrieveImage_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            RetrieveImagetMenu.IsOpen = false;
            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("No Loaded Image path found to Retrieve Images..!\nPlease Load Images from Menu.", "Not found", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("No Images found to retrieve to path.", 0);
                return;
            }
            if (!CheckCSVFileLoaded())
                return;


            string type = (sender as Telerik.Windows.Controls.RadMenuItem).Header.ToString() == "Move Images" ? "Move" : "Copy";
            ClassSelectionWindow selectionWindow = new ClassSelectionWindow(this, type);
            selectionWindow.Owner = this;
            selectionWindow.ShowDialog();
        }

        /// <summary>
        /// Function to start CSV Export when Export to csv menu selected
        /// </summary>
        private void CSVExport_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if(settings.ClassType == EnumClassType.Segregation)
            {
                BackgroundWorker bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += CSVExportSegregatedImages;
                bgWorker.RunWorkerAsync();

                return;
            }
            else if(settings.ClassType == EnumClassType.Rectangle || settings.ClassType == EnumClassType.Polyline)
            {
                BackgroundWorker bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                bgWorker.DoWork += CSVExportRectangleImages;
                bgWorker.RunWorkerAsync();

                return;
            }

            bool bIsExportSavedFile = false;
            bool bIsExportRawFile = false;
            if (!bIsFormatFile && (ImageMenuList == null || ImageMenuList.Count == 0))
            {
                System.Windows.MessageBox.Show("Nothing to export..!", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (bIsFormatFile && (ImageMenuList == null || ImageMenuList.Count == 0))
            {
                //MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export Saved Format file into Output Folder?", "Export Format File", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (result == MessageBoxResult.No)
                //    return;
                //else
                //{
                bIsExportSavedFile = true;
                bIsExportRawFile = false;
                //}
            }
            else if (!bIsFormatFile && ImageMenuList.Count > 0)
            {
                //MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export as Raw CSV file into Output Folder?", "Export Raw file", MessageBoxButton.YesNo, MessageBoxImage.Question,
                //                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //if (result == MessageBoxResult.No)
                //    return;
                //else
                //{
                bIsExportSavedFile = false;
                bIsExportRawFile = true;
                //}
            }
            else if (ImageMenuList.Count > 0 && bIsFormatFile)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to export Saved Format file along with Raw CSV into Output Folder?", "Export Raw File", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                           MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);

                if (result == MessageBoxResult.No)
                {
                    bIsExportSavedFile = true;
                    bIsExportRawFile = false;
                }
                else
                {
                    bIsExportSavedFile = true;
                    bIsExportRawFile = true;
                }
            }

            bool bIsMultiFile = (sender.GetType().Name == "RadRibbonButton" && (sender as Telerik.Windows.Controls.RadRibbonButton).Name == "ExportButton") ||
                                (sender.GetType().Name == "RadMenuItem" && (sender as Telerik.Windows.Controls.RadMenuItem).Name == "MenuExportSingle") ? false : true;

            object[] args = { bIsExportSavedFile, bIsMultiFile, bIsExportRawFile };
            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_CSVFileExportData;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(args);
            OnWorkerMethodStart_withPercentage();
        }

        private void CSVSingleExport_Click(object sender, RoutedEventArgs e)
        {
            CSVExport_Click(sender, null);
        }

        /// <summary>
        /// Function to change the review images when corrction check box in class list is selected
        /// </summary>
        private void ClassCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ImageMenu curImageMenu = listBoxImages.SelectedItem as ImageMenu;
            ImageClass curImageClass = (sender as System.Windows.Controls.CheckBox).DataContext as ImageClass;

            if (curImageMenu == null || curImageClass == null)
                return;

            curImageClass.Reviewed = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
            curImageMenu.MenuItemBrush = SelectedImageBox.ListImageClass.ToList().Exists(item => item.Reviewed == true) ? ImageMenuBrushes[2] : ImageMenuBrushes[1];
            lvImageClass.SelectedIndex = SelectedImageBox.ListImageClass.IndexOf(curImageClass);
            RefreshListBoxImages();

            if (arrSeleImportData != null && arrSeleImportData.Length > 4)
                arrSeleImportData[4] = (sender as System.Windows.Controls.CheckBox).IsChecked.Value ? "Yes" : "No";
        }

        private void DataFolderSplitter_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load Images to Split Image Folder..!", "No Images Found", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            string strSplitOperation = "";
            if ((sender as Telerik.Windows.Controls.RadMenuItem).Header.ToString() == "Copy Folder")
                strSplitOperation = "Copy";
            else
                strSplitOperation = "Move";

            DataFolderSplitter windowDataDplit = new DataFolderSplitter(this, strSplitOperation);
            windowDataDplit.Owner = this;
            windowDataDplit.ShowDialog();            
        }

        private void WarningExpandMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Hide.Kind == MaterialDesignThemes.Wpf.PackIconKind.ArrowLeftBoldCircle)
            {
                gridMain.ColumnDefinitions[0].Width = new GridLength(0);
                Hide.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowRightBoldCircle;
                Hidebtn.ToolTip = "Expand SideBar";
            }

            else
            {
                gridMain.ColumnDefinitions[0].Width = new GridLength(310);
                Hide.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowLeftBoldCircle;
                Hidebtn.ToolTip = "Hide SideBar";
            }
        }

        private void SwitchWorkcellModeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Application will get reset. Do you wish to continue ?", "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            ResetApplication(true);

            ImageZoomBorder zoomBorder = border as ImageZoomBorder;
            if ((sender as Telerik.Windows.Controls.RadRibbonButton).Text == "Enable Workcell")
            {
                (sender as Telerik.Windows.Controls.RadRibbonButton).Text = "Disable Workcell";
                (sender as Telerik.Windows.Controls.RadRibbonButton).ToolTip = "Disable Workcell Mode";
                IsVisibleInWorkshellMode = Visibility.Collapsed;
                ImportWorkcellJsonButton.Visibility = Visibility.Visible;
                gridMain.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star); 
                gridMain.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
                gridMain.RowDefinitions[3].Height = new GridLength(0, GridUnitType.Pixel);
                gridImageAttribute.Visibility = Visibility.Collapsed;
                gSplitHorizontal.Visibility = Visibility.Collapsed;

                ExportButton.Visibility = Visibility.Collapsed;
                IsVisibleShapeQuickPallete = Visibility.Collapsed;
                rtabTools.IsEnabled = false;
                rtabAugment.IsEnabled = false;
                rtabAutopilot.IsEnabled = false;
                rtabAnalysis.IsEnabled = false;
                gridStats.Visibility = Visibility.Collapsed;
                bWorkCellMode = true;
                zoomBorder.bWorkcellMode = true;
            }
            else
            {
                (sender as Telerik.Windows.Controls.RadRibbonButton).Text = "Enable Workcell";
                (sender as Telerik.Windows.Controls.RadRibbonButton).ToolTip = "Enable Workcell Mode";
                IsVisibleInWorkshellMode = Visibility.Visible;
                ImportWorkcellJsonButton.Visibility = Visibility.Collapsed;
                gridImageAttribute.Height = 150;
                gSplitHorizontal.Height = 2;
                gridMain.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                gridMain.RowDefinitions[2].Height = new GridLength(2, GridUnitType.Pixel);
                gridMain.RowDefinitions[3].Height = new GridLength(150, GridUnitType.Pixel);
                gridImageAttribute.Visibility = Visibility.Visible;
                gSplitHorizontal.Visibility = Visibility.Visible;

                ExportButton.Visibility = Visibility.Visible;
                IsVisibleShapeQuickPallete = Visibility.Visible;
                rtabTools.IsEnabled = true;
                rtabAugment.IsEnabled = true;
                rtabAutopilot.IsEnabled = true;
                rtabAnalysis.IsEnabled = true;
                gridStats.Visibility = Visibility.Visible;
                bWorkCellMode = false;
                zoomBorder.bWorkcellMode = false;
            }
        }

        private void ImportWorkcellJSON_Click(object sender, RoutedEventArgs e)
        {
            AddMultipleImportFile windowSettings = new AddMultipleImportFile(this, "WorkCell");
            windowSettings.spSuggestion.Visibility = settings.ImportFilePath != null && settings.ImportFilePath.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            windowSettings.Owner = this;
            windowSettings.ShowDialog();
        }

        private void RemoveMultipleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (TotalMultiClassLabelled <= 0)
            {
                System.Windows.MessageBox.Show("Labelled images not found..!\nPlease Load Images and Import Datasheets and continue.", "Overlays not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            MessageBoxResult result = System.Windows.MessageBox.Show("Please confirm to start removing multiple overlays?", "Remove Overlays", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_RemoveMultipleOverlays;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStart_withPercentage();
        }

        private void ExportSegregateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (TotalMultiClassLabelled <= 0)
            {
                System.Windows.MessageBox.Show("Segregated images not found..!\nPlease export after segregation or Import Segregated CSV Datasheet.", "Data not found", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_ExportSegregatedImages;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStart_withPercentage();
        }

        private void DataSplit_Click(object sender, RoutedEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load labelled/segregated Images to Split Train/Val/Test set..!", "No Images Found", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None);
                return;
            }
            if (TotalMultiClassLabelled <= 0)
            {
                System.Windows.MessageBox.Show("Labelled/Segregated images not found..!", "No Images", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
                return;
            }
            if(TotalCorrectionImages > 0)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(TotalCorrectionImages + " Correction Images found in loaded dataset. Do you want to continue without correction image added in train/val/test dataset?" +
                    "\nClick on \"Yes\" to continue and \"No\" to cancel this and go for Image correction.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (result == MessageBoxResult.No)
                    return;
            }

            string strSplitOperation = "Copy";
            DataFolderSplitter windowDataDplit = new DataFolderSplitter(this, strSplitOperation);
            windowDataDplit.Owner = this;
            windowDataDplit.ShowDialog();
        }

        private void btnAugmentation_Click(object sender, RoutedEventArgs e)
        {
            WindowAugmentation windowAugmentation = new WindowAugmentation(this);
            windowAugmentation.Owner = this;
            windowAugmentation.ShowDialog();
        }

        private void SegregateAllButton_Click(object sender, RoutedEventArgs e)
        {
            SegregateAllImageWindow segregateAllImage = new SegregateAllImageWindow(this);
            segregateAllImage.Owner = this;
            segregateAllImage.ShowDialog();
        }
        //private void QuickPallete_Expanded(object sender, Telerik.Windows.RadRoutedEventArgs e)
        //{
        //    if ((sender as Telerik.Windows.Controls.RadExpander).IsExpanded == true)
        //        (sender as Telerik.Windows.Controls.RadExpander).ToolTip = "Hide Quick Palette";
        //    else
        //        (sender as Telerik.Windows.Controls.RadExpander).ToolTip = "Show Quick Palette";
        //}
    }

    public class WidthConverter : IValueConverter
    {
        public object Convert(object o, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Controls.ListView l = o as System.Windows.Controls.ListView;
            GridView g = l.View as GridView;
            double total = 0;
            for (int i = 0; i < g.Columns.Count - 1; i++)
            {
                total += g.Columns[i].Width;
            }
            return (l.ActualWidth - total);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertVisibilityConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                Visibility vis = (Visibility)value;
                return vis == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            }
            throw new InvalidOperationException();
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                bool vis = (bool)value;
                return vis == true ? Visibility.Visible : Visibility.Collapsed;
            }
            throw new InvalidOperationException();
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibilityToBoolConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool))
            {
                Visibility vis = (Visibility)value;
                return vis == Visibility.Visible ? true : false;
            }
            throw new InvalidOperationException();
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

