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
    /// Interaction logic for ProjectConfigWindow.xaml
    /// </summary>
    public partial class ProjectConfigWindow : Window
    {
        MainWindow app;
        string ManageType;
        public event PropertyChangedEventHandler PropertyChanged;
        string ProjectKey;
        string ClassFilePath;
        string Architecture;

        public ProjectConfigWindow(MainWindow app, string Type)
        {
            InitializeComponent();
            this.app = app;
            ManageType = Type;
            DataContext = this;
            InitializeControls();
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
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
            listProjectClass = new ObservableCollection<ImageClass>();
            txtProjectName.Text = "";
            txtStation.Text = "";
            txtTotalClass.Text = "";
            txtTotalLines.Text = "";
            txtClassName.Text = "";
            txtClassAlias.Text = "";
            lvProjectClass.ItemsSource = ListProjectClass;
            radAny.IsChecked = false;
            radRecta.IsChecked = false;
            radPoly.IsChecked = false;
            radSegregation.IsChecked = false;
            gridIllumination.Visibility = Visibility.Collapsed;
            cmbIlluminations.ItemsSource = app.settings.ListIlluminations;
            cmbIlluminations.SelectedIndex = 0;

            cmbProject.Items.Clear();
            List<int> listProjectKey = app.settings.dictProjectList.Keys.Select(item => Convert.ToInt32(item.Replace("P", ""))).OrderBy(x => x).ToList();
            foreach (int key in listProjectKey)
            {
                string strKey = "P" + key;
                if (app.settings.dictProjectList[strKey] != "NONE")
                    cmbProject.Items.Add(app.settings.dictProjectList[strKey]);
            }
            cmbProject.SelectedIndex = -1;
            cmbProject.Focus();

            if (ManageType == "New")
            {
                AddProject.Visibility = Visibility.Visible;
                lblHeader.Text = "Add Project";
                btnGenerate.Content = "Generate";
                btnGenerate.ToolTip = "Generate Project and Close";
                lblProject.Content = "Project Name : ";
                btnDelete.Visibility = Visibility.Collapsed;
                cmbArchitecture.ItemsSource = app.settings.ListArchitectures;
                cmbArchitecture.SelectedIndex = 0;
                radSegregation.IsChecked = cmbArchitecture.SelectedItem.ToString().Contains(app.settings.ClassificationAlias) ||
                                cmbArchitecture.SelectedItem.ToString().Contains(app.settings.PatchcoreAlias) ? true : false;
                radRecta.IsChecked = cmbArchitecture.SelectedItem.ToString().Contains(app.settings.DetectionAlias) ? true : false;
                radPoly.IsChecked = cmbArchitecture.SelectedItem.ToString().Contains(app.settings.SegmentationAlias) ? true : false;
                SetControls(true);
                cmbProject.IsEnabled = true;
            }
            else if(ManageType == "Edit")
            {                
                AddProject.Visibility = Visibility.Collapsed;
                cmbArchitecture.ItemsSource = null;
                SetControls(false); 
                lblHeader.Text = "Edit Project";
                
                btnGenerate.Content = "Modify";
                btnGenerate.ToolTip = "Click to modify project";
                lblProject.Content = "Select Project : ";
                btnDelete.Visibility = Visibility.Visible;
            }
        }

        private void SetControls(bool bIsEnable = true)
        {
            cmbProject.IsEnabled = !bIsEnable;
            txtStation.IsEnabled = bIsEnable;
            txtProjectName.IsEnabled = bIsEnable;
            txtTotalClass.IsEnabled = bIsEnable;
            txtTotalLines.IsEnabled = bIsEnable;
            //spType.IsEnabled = ManageType == "Update" && ProjectKey =="P1"? false : bIsEnable;
            spClass.IsEnabled = bIsEnable;
            radAny.IsEnabled = bIsEnable;
            cmbIlluminations.IsEnabled = bIsEnable;
            SetListBoxProperty(bIsEnable);
        }

        private void SetListBoxProperty(bool isEnable = false)
        {
            var s = new Style(typeof(ListViewItem));
            var disableSetter = new Setter { Property = IsEnabledProperty, Value = isEnable };
            s.Setters.Add(disableSetter);
            lvProjectClass.ItemContainerStyle = s;
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        ObservableCollection<ImageClass> listProjectClass = new ObservableCollection<ImageClass>();
        public ObservableCollection<ImageClass> ListProjectClass
        {
            get
            {
                return listProjectClass;
            }
            set
            {
                listProjectClass = value;
                NotifyPropertyChanged("ListProjectClass");
            }
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void txtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if ((sender as TextBox).Text.Length > 1)
                e.Handled = true;

            if (regex.IsMatch(e.Text))
                e.Handled = true;
        }

        private void txtString_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z]+");
            if (regex.IsMatch(e.Text))
                e.Handled = true;
        }

        private void txtClassName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9_-]+");
            if (regex.IsMatch(e.Text))
                e.Handled = true;
        }

        public ImageClass selClassDetail = null;
        private void AddClass_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClassName.Text) || string.IsNullOrWhiteSpace(txtClassAlias.Text)){
                MessageBox.Show("Entry Fields should not be blank..!", "Empty", MessageBoxButton.OK, MessageBoxImage.Error);
                if (string.IsNullOrWhiteSpace(txtClassName.Text))
                    txtClassName.Focus();
                else
                    txtClassAlias.Focus();

                return;
            }

            if (string.IsNullOrWhiteSpace(txtTotalClass.Text) || Convert.ToInt32(txtTotalClass.Text.Trim()) == 0){
                MessageBox.Show("Total class should not be blank or zero..!", "Empty", MessageBoxButton.OK, MessageBoxImage.Error);
                txtTotalClass.Focus();
                return;
            }
            if (selClassDetail == null && ListProjectClass.Count + 1 > Convert.ToInt32(txtTotalClass.Text.Trim())){
                MessageBox.Show("You cannot add class more than total class count entered..!", "Overflow..!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(selClassDetail == null)
            {
                var listNumbers = Enumerable.Range(0, Convert.ToInt32(txtTotalClass.Text));
                string classID = ListProjectClass.Count > 0 ? listNumbers.Except(ListProjectClass.Select(item => Convert.ToInt32(item.ClassIndex))).Min().ToString() : "0";
                if (ListProjectClass.FirstOrDefault(temp => temp.ClassAlias.ToUpper() == txtClassAlias.Text.Trim().ToUpper()) != null ||
                    ListProjectClass.FirstOrDefault(temp => temp.ClassName.ToUpper() == txtClassName.Text.Trim().ToUpper()) != null)
                {
                    MessageBox.Show("Class Name/Class Alias Already exists..!", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtClassAlias.Focus();
                    return;
                }

                ImageClass curProjectClass = new ImageClass(classID, txtClassName.Text.Trim());
                curProjectClass.ClassAlias = txtClassAlias.Text.Trim();
                curProjectClass.ShapeCoordinates = chkIsRealDefect.IsChecked.Value ? "Fail" : "Pass";
                ListProjectClass.Add(curProjectClass);
                txtClassName.Text = "";
                txtClassAlias.Text = "";
                chkIsRealDefect.IsChecked = false;
                txtClassName.Focus();
                ListProjectClass = new ObservableCollection<ImageClass>(ListProjectClass.OrderBy(temp => Convert.ToInt32(temp.ClassIndex)));
                lvProjectClass.ItemsSource = ListProjectClass;
            }
            else
            {
                if (ListProjectClass.Select(s => s.ClassAlias.ToUpper()).Where(item => item != selClassDetail.ClassAlias.ToUpper()).Contains(txtClassAlias.Text.Trim().ToUpper()) ||
                    ListProjectClass.Select(s => s.ClassName.ToUpper()).Where(item => item != selClassDetail.ClassName.ToUpper()).Contains(txtClassName.Text.Trim().ToUpper()))
                {
                    MessageBox.Show("Class Name/Class Alias Already exists..!", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtClassAlias.Focus();
                    return;
                }

                selClassDetail.ClassName = txtClassName.Text.Trim();
                selClassDetail.ClassAlias = txtClassAlias.Text.Trim();
                selClassDetail.ShapeCoordinates = chkIsRealDefect.IsChecked.Value ? "Fail" : "Pass";
                txtClassName.Text = "";
                txtClassAlias.Text = "";
                chkIsRealDefect.IsChecked = false;
                lvProjectClass.Items.Refresh();
                selClassDetail = null;
            }            
        }

        private void btnRemoveClass_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ImageClass curProjectClass = (sender as System.Windows.Controls.Button).DataContext as ImageClass;
            if (curProjectClass != null)
                ListProjectClass.Remove(curProjectClass);

            for(int i = 0; i < ListProjectClass.Count; i++)
            {
                ListProjectClass[i].ClassIndex = i.ToString();
            }
            lvProjectClass.Items.Refresh();
        }

        private void btnEditClass_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button).DataContext == null)
                return;

            ImageClass curProjectClass = (sender as System.Windows.Controls.Button).DataContext as ImageClass;
            if (curProjectClass != null)
            {
                txtClassName.Text = curProjectClass.ClassName;
                txtClassAlias.Text = curProjectClass.ClassAlias;
                chkIsRealDefect.IsChecked = curProjectClass.ShapeCoordinates == "Fail" ? true : false;
                selClassDetail = curProjectClass;
            }
        }

        private void btnMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ListProjectClass.Count < 2)
                return;

            ImageClass curProjectClass = (sender as System.Windows.Controls.Button).DataContext as ImageClass;

            if((sender as System.Windows.Controls.Button).Name == "btnMoveUp"){

                if(Convert.ToInt32(curProjectClass.ClassIndex) > 0)
                {
                    string prevIndex = (Convert.ToInt32(curProjectClass.ClassIndex) - 1).ToString();
                    ImageClass prevImageClass = ListProjectClass.FirstOrDefault(item => item.ClassIndex == prevIndex);
                    if (prevImageClass != null)
                    {
                        string strAlias = prevImageClass.ClassAlias;
                        string strName = prevImageClass.ClassName;
                        string strRealDefect = prevImageClass.ShapeCoordinates;

                        prevImageClass.ClassAlias = curProjectClass.ClassAlias;
                        prevImageClass.ClassName = curProjectClass.ClassName;
                        prevImageClass.ShapeCoordinates = curProjectClass.ShapeCoordinates;

                        curProjectClass.ClassAlias = strAlias;
                        curProjectClass.ClassName = strName;
                        curProjectClass.ShapeCoordinates = strRealDefect;
                    }
                    lvProjectClass.SelectedItem = prevImageClass;
                }
            }
            else if((sender as System.Windows.Controls.Button).Name == "btnMoveDown")
            {
                if (Convert.ToInt32(curProjectClass.ClassIndex) < ListProjectClass.Count)
                {
                    string nextIndex = (Convert.ToInt32(curProjectClass.ClassIndex) + 1).ToString();
                    ImageClass nextImageClass = ListProjectClass.FirstOrDefault(item => item.ClassIndex == nextIndex);
                    if (nextImageClass != null)
                    {
                        string strAlias = nextImageClass.ClassAlias;
                        string strName = nextImageClass.ClassName;
                        string strRealDefect = nextImageClass.ShapeCoordinates;
                        
                        nextImageClass.ClassAlias = curProjectClass.ClassAlias;
                        nextImageClass.ClassName = curProjectClass.ClassName;
                        nextImageClass.ShapeCoordinates = curProjectClass.ShapeCoordinates;

                        curProjectClass.ClassAlias = strAlias;
                        curProjectClass.ClassName = strName;
                        curProjectClass.ShapeCoordinates = strRealDefect;
                    }
                    lvProjectClass.SelectedItem = nextImageClass;
                }
            }
            lvProjectClass.Items.Refresh();
        }

        private void txtClassAlias_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtClassAlias.Text) && e.Key == Key.Enter)
                AddClass_PreviewMouseLeftButtonDown(null, null);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }

        private bool IsValidEntries()
        {
            if (cmbProject.SelectedIndex == -1)
            {
                MessageBox.Show("Please select Project..!", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Error);
                cmbProject.Focus();
                return false;
            }
            if (cmbArchitecture.SelectedIndex == -1)
            {
                MessageBox.Show("Please select Architecture..!", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Error);
                cmbArchitecture.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtStation.Text)){
                MessageBox.Show("Please enter Station Name..!", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStation.Focus();
                return false;
            }            

            if(ListProjectClass.Count == 0){
                MessageBox.Show("Class Details cannot be empty..!", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Error);
                txtClassName.Focus();
                return false;
            }
            if(!radPoly.IsChecked.Value && !radRecta.IsChecked.Value && !radAny.IsChecked.Value && !radSegregation.IsChecked.Value) {
                MessageBox.Show("Please choose Annotation type..!", "Select", MessageBoxButton.OK, MessageBoxImage.Error);
                radRecta.Focus();
                return false;
            }

            if(!cmbArchitecture.SelectedItem.ToString().Contains(app.settings.PatchcoreAlias) && cmbIlluminations.SelectedItem == null)
            {
                MessageBox.Show("Please select Illumination Type for project..!", "Select", MessageBoxButton.OK, MessageBoxImage.Error);
                cmbIlluminations.Focus();
                return false;
            }

            if(app.settings.dictProjectList.Values.Contains(cmbProject.SelectedItem.ToString(), StringComparer.CurrentCultureIgnoreCase))
            {
                if(ManageType == "Update" && cmbArchitecture.Text == Architecture)
                {
                    return true;
                }
                string strProjectName = app.settings.dictProjectList.FirstOrDefault(item => item.Value.ToUpper() == cmbProject.SelectedItem.ToString().ToUpper()).Value;
                string strClassFile = GetClassPathfromName(strProjectName);
                if(strClassFile != null)
                {
                    MessageBox.Show("Project name with Architecture already exists..! Please change project or Architecture and continue..", "Exists", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }                
            }
            return true;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as System.Windows.Controls.Button).Content.ToString().Contains("Modify"))
                {
                    if (cmbProject.SelectedIndex == -1 || cmbArchitecture.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select Project/Architecture then continue..!", "Invalid", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Architecture = cmbArchitecture.Text.Trim();
                    btnGenerate.Content = "Save Project";
                    btnDelete.Visibility = Visibility.Collapsed;
                    btnGenerate.ToolTip = "Save modified project";
                    ManageType = "Update";
                    SetControls(true);
                    txtStation.Focus();
                    return;
                }
                else if ((sender as System.Windows.Controls.Button).Content.ToString().Contains("Generate"))
                {
                    if (!IsValidEntries())
                        return;

                    string ProjectName = cmbProject.SelectedItem.ToString();
                    IniFile iniWrite = new IniFile(app.settings.StatsFilePath + @"Genie_" + ProjectName + "_" + cmbArchitecture.Text.ToString() + ".ini");
                    iniWrite.WriteValue("ClassInfo", "Station", txtStation.Text.Trim());
                    iniWrite.WriteValue("ClassInfo", "Project", "P" + app.settings.dictProjectList.Count.ToString());
                    iniWrite.WriteValue("ClassInfo", "ProjectName", ProjectName);
                    iniWrite.WriteValue("ClassInfo", "Architecture", cmbArchitecture.Text.ToString());
                    iniWrite.WriteValue("ClassInfo", "Classes", ListProjectClass.Count.ToString());
                    iniWrite.WriteValue("ClassInfo", "Lines", txtTotalLines.Text.ToString());

                    if (cmbArchitecture.SelectedItem.ToString().Contains(app.settings.PatchcoreAlias))
                        iniWrite.WriteValue("ClassInfo", "IlluminationType", cmbIlluminations.SelectedItem.ToString());

                    string type = radRecta.IsChecked.Value ? "Rectangle" : radPoly.IsChecked.Value ? "Polyline" : radSegregation.IsChecked.Value ? "Segregation" : "Any";
                    iniWrite.WriteValue("Annotation", "Type", type);
                    
                    foreach (ImageClass curClass in ListProjectClass)
                        iniWrite.WriteValue("Class", "C" + curClass.ClassIndex, curClass.ClassName + "(" + curClass.ClassAlias + ")");

                    var listPassClass = ListProjectClass.Where(item => item.ShapeCoordinates == "Pass");
                    var listFailClass = ListProjectClass.Where(item => item.ShapeCoordinates == "Fail");

                    foreach (ImageClass curClass in listPassClass)
                        iniWrite.WriteValue("Pass", "C" + curClass.ClassIndex, curClass.ClassAlias);
                    foreach (ImageClass curClass in listFailClass)
                        iniWrite.WriteValue("Fail", "C" + curClass.ClassIndex, curClass.ClassAlias);

                    int lineCount = string.IsNullOrWhiteSpace(txtTotalLines.Text) ? 0 : Convert.ToInt32(txtTotalLines.Text.Trim());
                    for (int i = 1; i < lineCount + 1; i++)
                        iniWrite.WriteValue("Line", "L" + i, "PM " + i);

                    if(app.settings.dictProjectList.Count == 0)
                        app.settings.dictProjectList.Add("P" + app.settings.dictProjectList.Count.ToString(), "NONE");
                    if (!app.settings.dictProjectList.Values.Contains(ProjectName))
                        app.settings.dictProjectList.Add("P" + app.settings.dictProjectList.Count.ToString(), ProjectName);
                    app.settings.WriteConfigSettings();

                    CreateFolderStructureinConfigPath();
                    MessageBox.Show("Project successfully generated and loaded in application..", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    Utilities.LogMessage("Project successfully generated and loaded in application", 0);
                    ManageType = "New";
                    InitializeControls();
                }
                else if ((sender as System.Windows.Controls.Button).Content.ToString().Contains("Save") && !string.IsNullOrEmpty(ClassFilePath))
                {
                    if (!IsValidEntries())
                        return;

                    if (System.IO.File.Exists(ClassFilePath))
                        System.IO.File.Delete(ClassFilePath);

                    string ProjectName = cmbProject.SelectedItem.ToString();
                    ClassFilePath = app.settings.StatsFilePath + @"Genie_" + ProjectName + "_" + cmbArchitecture.Text.ToString() + ".ini";
                    IniFile iniWrite = new IniFile(ClassFilePath);
                    iniWrite.WriteValue("ClassInfo", "Station", txtStation.Text.Trim());
                    iniWrite.WriteValue("ClassInfo", "Project", ProjectKey);
                    iniWrite.WriteValue("ClassInfo", "ProjectName", ProjectName);
                    iniWrite.WriteValue("ClassInfo", "Architecture", cmbArchitecture.Text.ToString());
                    iniWrite.WriteValue("ClassInfo", "Classes", ListProjectClass.Count.ToString());
                    iniWrite.WriteValue("ClassInfo", "Lines", txtTotalLines.Text.ToString());

                    if (cmbArchitecture.SelectedItem.ToString().Contains(app.settings.PatchcoreAlias))
                        iniWrite.WriteValue("ClassInfo", "IlluminationType", cmbIlluminations.SelectedItem.ToString());

                    string type = radRecta.IsChecked.Value ? "Rectangle" : radPoly.IsChecked.Value ? "Polyline" : radSegregation.IsChecked.Value ? "Segregation" : "Any";
                    iniWrite.WriteValue("Annotation", "Type", type);

                    foreach (ImageClass curClass in ListProjectClass)
                        iniWrite.WriteValue("Class", "C" + curClass.ClassIndex, curClass.ClassName + "(" + curClass.ClassAlias + ")");

                    var listPassClass = ListProjectClass.Where(item => item.ShapeCoordinates == "Pass");
                    var listFailClass = ListProjectClass.Where(item => item.ShapeCoordinates == "Fail");

                    foreach (ImageClass curClass in listPassClass)
                        iniWrite.WriteValue("Pass", "C" + curClass.ClassIndex, curClass.ClassAlias);
                    foreach (ImageClass curClass in listFailClass)
                        iniWrite.WriteValue("Fail", "C" + curClass.ClassIndex, curClass.ClassAlias);

                    int lineCount = string.IsNullOrWhiteSpace(txtTotalLines.Text) ? 0 : Convert.ToInt32(txtTotalLines.Text.Trim());
                    for (int i = 1; i < lineCount + 1; i++)
                        iniWrite.WriteValue("Line", "L" + i, "PM " + i);

                    if (ClassFilePath == app.settings.ClassFilePath)
                    {
                        app.settings.ReadClassFileConfig();
                        app.InitializeComboBox();
                    }

                    CreateFolderStructureinConfigPath();
                    MessageBox.Show("Selected Project modified successfully..", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    Utilities.LogMessage("Selected Project modified successfully.", 0);
                    ManageType = "Edit";
                    InitializeControls();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Something went wrong..!\nError while Generating Project config file." + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ProjectConfigWindow::btnGenerate_Click: " + ex.Message, 1);
            }
        }

        private void CreateFolderStructureinConfigPath()
        {
            string strProjectname = cmbProject.SelectedItem.ToString();
            string strArchitecture = cmbArchitecture.Text.ToString();
            string strValDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture + @"\" + app.settings.valFolder;
            string strTrainDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture + @"\" + app.settings.trainFolder;
            string strSourceDataSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture + @"\" + app.settings.sourceFolder;
            string strTestSetPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture + @"\" + app.settings.testsetFolder;
            string strCSVExportPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture + @"\" + app.settings.CSVExportFolder;

            if (!strArchitecture.Contains(app.settings.PatchcoreAlias) && !Directory.Exists(strValDataSetPath))
                Directory.CreateDirectory(strValDataSetPath);

            if (!Directory.Exists(strTrainDataSetPath))
                Directory.CreateDirectory(strTrainDataSetPath);

            if (!Directory.Exists(strTestSetPath))
                Directory.CreateDirectory(strTestSetPath);

            if (!Directory.Exists(strSourceDataSetPath))
                Directory.CreateDirectory(strSourceDataSetPath);

            if (!Directory.Exists(strCSVExportPath))
                Directory.CreateDirectory(strCSVExportPath);
        }

        private void cmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProject.SelectedIndex == -1 || ManageType == "New")
                return;

            string[] arrArchitecture = GetArchitectureArray(cmbProject.SelectedItem.ToString());
            cmbArchitecture.ItemsSource = arrArchitecture;
            if (arrArchitecture.Length > 0)
            {
                cmbArchitecture.SelectionChanged -= cmbArchitecture_SelectionChanged;
                cmbArchitecture.SelectedIndex = 0;
                cmbArchitecture.SelectionChanged += cmbArchitecture_SelectionChanged;
                cmbArchitecture_SelectionChanged(null, null);
            }
        }

        private string[] GetArchitectureArray(string strProjectName)
        {
            List<string> listArchitecture = new List<string>();
            string[] classIniFiles = System.IO.Directory.GetFiles(app.settings.StatsFilePath, "*Genie*.ini", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < classIniFiles.Length; i++)
            {
                IniFile iniLabel = new IniFile(classIniFiles[i]);
                if (iniLabel.ReadValue("ClassInfo", "ProjectName", "") == strProjectName)
                {
                    string strArch = iniLabel.ReadValue("ClassInfo", "Architecture", "");
                    if (strArch != "" & !listArchitecture.Contains(strArch))
                        listArchitecture.Add(strArch);
                }
            }
            return listArchitecture.ToArray();
        }

        private void radClassType_Checked(object sender, RoutedEventArgs e)
        {
            if (ManageType != "Edit")
                return;

            ClassFilePath = GetClassPath(sender);
            if(ClassFilePath != null)
            {
                IniFile iniRead = new IniFile(ClassFilePath);
                txtProjectName.Text = cmbProject.SelectedItem.ToString();
                txtStation.Text = iniRead.ReadValue("ClassInfo", "Station", "");
                txtTotalClass.Text = iniRead.ReadValue("ClassInfo", "Classes", "");
                txtTotalLines.Text = iniRead.ReadValue("ClassInfo", "Lines", "");

                string type = iniRead.ReadValue("Annotation", "Type", "Any");
                if (type == "Rectangle")
                    radRecta.IsChecked = true;
                else if (type == "Polyline")
                    radPoly.IsChecked = true;
                else if (type == "Segregation")
                    radSegregation.IsChecked = true;
                else
                    radAny.IsChecked = true;

                int classCount = Convert.ToInt32(txtTotalClass.Text);
                ListProjectClass = new ObservableCollection<ImageClass>();
                for (int i = 0, j = 0; i < classCount; i++, j++)
                {
                    //If class index present in random number & it is to limit loop for 200
                    if (j > 99)
                        break;
                    string strClassName = iniRead.ReadValue("Class", String.Format("C{0}", j), "");
                    if (String.IsNullOrEmpty(strClassName))
                    {
                        i--;
                        continue;
                    }
                    string className = strClassName.Split('(', ')').Length > 0 ? strClassName.Split('(', ')')[0] : strClassName.Split('(', ')')[0]; 

                    string classAlias = strClassName.Split('(', ')').Length > 1 ? strClassName.Split('(', ')')[1] : strClassName.Split('(', ')')[0];
                    ListProjectClass.Add(new ImageClass(j.ToString(), className)
                    {
                        ClassAlias = classAlias
                    });
                }
                lvProjectClass.ItemsSource = ListProjectClass;
            }
        }

        private string GetClassPath(object sender)
        {
            string[] classIniFiles = System.IO.Directory.GetFiles(app.settings.StatsFilePath, "*Genie*.ini", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < classIniFiles.Length; i++)
            {
                IniFile iniLabel = new IniFile(classIniFiles[i]);
                if (ProjectKey == "P1") {
                    if (iniLabel.ReadValue("Annotation", "Type", "") == (sender as System.Windows.Controls.RadioButton).Content.ToString() && iniLabel.ReadValue("ClassInfo", "Project", "") == ProjectKey)
                        return classIniFiles[i];
                }
                else if (iniLabel.ReadValue("ClassInfo", "Project", "") == ProjectKey)
                    return classIniFiles[i];
            }
            return null;
        }

        private string GetClassPathfromName(string strProjectName)
        {
            string[] classIniFiles = System.IO.Directory.GetFiles(app.settings.StatsFilePath, "*Genie*.ini", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < classIniFiles.Length; i++)
            {
                IniFile iniLabel = new IniFile(classIniFiles[i]);
                if (iniLabel.ReadValue("ClassInfo", "ProjectName", "") == strProjectName && iniLabel.ReadValue("ClassInfo", "Architecture", "") == cmbArchitecture.SelectedItem.ToString())
                    return classIniFiles[i];
            }
            return null;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbProject.SelectedIndex == -1 || cmbArchitecture.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select Project/Architecture then continue..!", "Invalid", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult res = System.Windows.MessageBox.Show("Are you sure you want to delete this project?", "Waring", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.No)
                    return;

                ProjectKey = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Key;
                object obj = radRecta.IsChecked.Value ? radRecta : radPoly.IsChecked.Value ? radPoly : radAny;
                string strProjectName = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Value;
                string classFilePath = GetClassPathfromName(strProjectName);
                //string classFilePath = GetClassPath(obj);

                string strProjectname = cmbProject.SelectedItem.ToString();
                string strArchitecture = cmbArchitecture.SelectedItem.ToString();
                if (ProjectKey == app.settings.CurrentProject && strArchitecture == app.settings.Architecture && ((app.listBoxImages.ItemsSource != null && app.listBoxImages.Items.Count > 0)
                            || (app.settings.ImportFilePath != null && app.settings.ImportFilePath.Length > 0)))
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("Application will Reset while delete this project.. \nDo you want to continue?", "Waring", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.No)
                        return;
                    else
                        app.ResetApplication();
                }

                if (classFilePath != null)
                    File.Delete(classFilePath);

                string[] arrArchitecture = GetArchitectureArray(strProjectname);
                if (arrArchitecture.Length == 0)
                    app.settings.dictProjectList.Remove(ProjectKey);
                if (app.settings.CurrentProject == ProjectKey && strArchitecture == app.settings.Architecture && app.settings.dictProjectList.Keys.Contains("P0"))
                {
                    app.settings.CurrentProject = "P0";
                    app.settings.Architecture = "";
                    app.settings.ClassFilePath = "";
                    app.settings.ReadClassFileConfig();
                    app.InitializeComboBox();
                }
                app.settings.WriteConfigSettings();

                string strProjectPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + strArchitecture;
                if (Directory.Exists(strProjectPath))
                    Directory.Delete(strProjectPath, true);

                if (Directory.Exists(app.ConfigFilePath + @"Admin\" + strProjectname) && Directory.GetDirectories(app.ConfigFilePath + @"Admin\" + strProjectname).Length == 0)
                    Directory.Delete(app.ConfigFilePath + @"Admin\" + strProjectname);

                ManageType = "Edit";
                InitializeControls();
                Utilities.LogMessage("Selected Project has been deleted.", 0);
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("Error while deleting the project: " + ex.Message, 9);
            }
        }

        private void cmbArchitecture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbArchitecture.SelectedIndex != -1)
            {
                if (cmbArchitecture.SelectedItem.ToString().Contains(app.settings.DetectionAlias))
                    radRecta.IsChecked = true;
                else if (cmbArchitecture.SelectedItem.ToString().Contains(app.settings.SegmentationAlias))
                    radPoly.IsChecked = true;
                else
                    radSegregation.IsChecked = true;

                if (cmbArchitecture.SelectedItem.ToString().Contains(app.settings.PatchcoreAlias))
                    gridIllumination.Visibility = Visibility.Visible;
                else
                    gridIllumination.Visibility = Visibility.Collapsed;
            }

            if ((ManageType == "Edit" && cmbProject.SelectedIndex == -1) || cmbArchitecture.SelectedIndex == -1)
                return;

            if (ManageType != "Edit")
                return;

            ProjectKey = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Key;           

            string strProjectName = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Value;
            ClassFilePath = GetClassPathfromName(strProjectName);
            if (ClassFilePath != null)
            {
                IniFile iniRead = new IniFile(ClassFilePath);
                txtProjectName.Text = cmbProject.SelectedItem.ToString();
                txtStation.Text = iniRead.ReadValue("ClassInfo", "Station", "");
                txtTotalClass.Text = iniRead.ReadValue("ClassInfo", "Classes", "");
                txtTotalLines.Text = iniRead.ReadValue("ClassInfo", "Lines", "");

                string type = iniRead.ReadValue("Annotation", "Type", "Any");
                if (type == "Rectangle")
                    radRecta.IsChecked = true;
                else if (type == "Polyline")
                    radPoly.IsChecked = true;
                else if (type == "Segregation")
                    radSegregation.IsChecked = true;
                else
                    radAny.IsChecked = true;

                string strIllumination = iniRead.ReadValue("ClassInfo", "IlluminationType", "");
                if (strIllumination != "" && app.settings.ListIlluminations.Contains(strIllumination))
                    cmbIlluminations.SelectedItem = strIllumination;
                else
                    cmbIlluminations.SelectedItem = null;

                //radRecta.IsChecked = true;
                int classCount = Convert.ToInt32(txtTotalClass.Text);
                ListProjectClass = new ObservableCollection<ImageClass>();
                for (int i = 0, j = 0; i < classCount; i++, j++)
                {
                    //If class index present in random number & it is to limit loop for 200
                    if (j > 99)
                        break;
                    string strClassName = iniRead.ReadValue("Class", String.Format("C{0}", j), "");
                    if (String.IsNullOrEmpty(strClassName))
                    {
                        i--;
                        continue;
                    }
                    string className = strClassName.Split('(', ')').Length > 0 ? strClassName.Split('(', ')')[0] : strClassName.Split('(', ')')[0];
                    string classAlias = strClassName.Split('(', ')').Length > 1 ? strClassName.Split('(', ')')[1] : strClassName.Split('(', ')')[0];

                    string strPass = iniRead.ReadValue("Pass", String.Format("C{0}", j), "");
                    string strFail = iniRead.ReadValue("Fail", String.Format("C{0}", j), "");
                    bool isRealDefect = strFail != "" ? true : false;
                    ListProjectClass.Add(new ImageClass(j.ToString(), className)
                    {
                        ClassAlias = classAlias,
                        ShapeCoordinates = isRealDefect? "Fail" : "Pass"
                    }) ;
                }
                lvProjectClass.ItemsSource = ListProjectClass;
            }
            else
                InitializeControls();
        }

        private void AddProject_Click(object sender, MouseButtonEventArgs e)
        {
            AddProjectWindow curAddProject = new AddProjectWindow(this);
            curAddProject.Owner = this;
            curAddProject.ShowDialog();
            cmbProject.Items.Refresh();
            string project = curAddProject.strProjectName;
            cmbProject.SelectedItem = project;
        }
    }
}
