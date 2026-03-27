using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        MainWindow app;
        public bool bIsSavePathLoad;
        EnumClassType SelectedClassType;
        string SelectedMode;
        string ProjectKey = string.Empty;
        string Architecture;

        public ConfigurationWindow(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            InitializeControls();
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

        private void InitializeControls()
        {
            txtExportCSVPath.Text = app.settings.CSVExportPath;
            txtModelPath.Text = app.settings.ModelPath;
            txtDefaultRad.Text = app.settings.DefaultRadius.ToString();

            List<int> listProjectKey = app.settings.dictProjectList.Keys.Select(item => Convert.ToInt32(item.Replace("P", ""))).OrderBy(x => x).ToList();
            foreach(int key in listProjectKey)
            {
                string strKey = "P" + key;
                cmbProject.Items.Add(app.settings.dictProjectList[strKey]);
            }
            //cmbProject.ItemsSource = app.settings.dictProjectList.Values;
            cmbProject.SelectionChanged -= cmbProject_SelectionChanged;
            cmbProject.SelectedItem = app.settings.dictProjectList.FirstOrDefault(item => item.Key == app.settings.CurrentProject.ToString()).Value;
            ProjectKey = cmbProject.SelectedItem != null? app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Key : "";
            cmbProject.SelectionChanged += cmbProject_SelectionChanged;

            InitializeProjectControls();
            txtClassPath.Text = app.settings.ClassFilePath;
            Architecture = app.settings.Architecture;
            string[] arrArchitecture = cmbProject.SelectedItem != null? GetArchitectureArray(cmbProject.SelectedItem.ToString()) : null;
            cmbArchitecture.ItemsSource = arrArchitecture;
            cmbArchitecture.SelectionChanged -= cmbArchitecture_SelectionChanged;
            cmbArchitecture.SelectedItem = app.settings.Architecture;
            cmbArchitecture.SelectionChanged += cmbArchitecture_SelectionChanged;

            if (app.settings.ClassType == EnumClassType.Rectangle)
            {
                chkRectangle.IsChecked = true;
                radRecta.IsChecked = true;
            }
            else if(app.settings.ClassType == EnumClassType.Polyline)
            {
                chkPolygon.IsChecked = true;
                radPoly.IsChecked = true;
            }
            else if (app.settings.ClassType == EnumClassType.Segregation)
            {
                radSegregation.IsChecked = true;
            }

            if (app.settings.ApplicationMode == "Normal")
                radNormal.IsChecked = true;
            else
                radTest.IsChecked = true;

            chkValidationStats.IsChecked = app.settings.blnValidationStat;
            toggleValidateID.IsChecked = app.settings.bIsValidatewithID;
            SelectedClassType = app.settings.ClassType;
            SelectedMode = app.settings.ApplicationMode;
            sliderZoom.Value = app.settings.ZoomLevel;
        }

        private void InitializeProjectControls()
        {
            spType.IsEnabled = false;
            radRecta.IsChecked = false;
            radPoly.IsChecked = false;
            radAny.IsChecked = false;
            radSegregation.IsChecked = false;
            txtClassPath.Text = "";
            foreach (System.Windows.Controls.CheckBox chkBox in spChkTools.Children)
                chkBox.IsChecked = false;

            //if (ProjectKey == "P1"){
            //    spType.IsEnabled = true;
            //    radAny.IsEnabled = false;
            //    radSegregation.IsEnabled = false;
            //}
            //else
            //    radAny.IsChecked = true;
        }

        private void btnPickClassPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDiag = new OpenFileDialog();
            openFileDiag.Filter = "ini file|*.ini";
            openFileDiag.Multiselect = false;
            DialogResult result = openFileDiag.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                txtClassPath.Text = openFileDiag.FileName; 
        }

        private void btnExportCSVPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = txtExportCSVPath.Text;

            DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                txtExportCSVPath.Text = folderDialog.SelectedPath;
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            int defRad = txtDefaultRad.Text.Trim() == "" ? 0 : Convert.ToInt32(txtDefaultRad.Text.Trim());
            SelectedMode = radNormal.IsChecked.Value ? "Normal" : "Test";
            if (SelectedClassType == app.settings.ClassType && txtExportCSVPath.Text.Trim() == app.settings.CSVExportPath && txtModelPath.Text.Trim() == app.settings.ModelPath &&
                txtZoomLevel.Text == app.settings.ZoomLevel + "X" && app.settings.blnValidationStat == chkValidationStats.IsChecked && app.settings.DefaultRadius == defRad &&
                ProjectKey == app.settings.CurrentProject && Architecture == app.settings.Architecture && SelectedMode == app.settings.ApplicationMode && app.settings.bIsValidatewithID == toggleValidateID.IsChecked)
            {
                this.Close();
                return;
            }

            if((ProjectKey != app.settings.CurrentProject || Architecture != app.settings.Architecture) && ((app.listBoxImages.ItemsSource != null && app.listBoxImages.Items.Count > 0)
                        || (app.settings.ImportFilePath != null && app.settings.ImportFilePath.Length > 0)))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Application will Reset while changing project.. \nDo you want to continue?", "Waring", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if(result == MessageBoxResult.No){
                    if(ProjectKey != app.settings.CurrentProject)
                    {
                        cmbProject.SelectionChanged -= cmbProject_SelectionChanged;
                        cmbProject.SelectedValue = app.settings.dictProjectList.FirstOrDefault(item => item.Key == app.settings.CurrentProject.ToString()).Value;
                        cmbProject.SelectionChanged += cmbProject_SelectionChanged;
                        cmbProject_SelectionChanged(null, null);
                    }

                    cmbArchitecture.SelectionChanged -= cmbArchitecture_SelectionChanged;
                    cmbArchitecture.SelectedValue = app.settings.Architecture;
                    cmbArchitecture.SelectionChanged += cmbArchitecture_SelectionChanged;
                    cmbArchitecture_SelectionChanged(null, null);
                    
                    return;
                }
                else {
                    app.bIsModelLoad = false;
                    app.ResetApplication();
                }
            }

            if (SaveConfiguration())
            {
                System.Windows.MessageBox.Show("Changes made saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Utilities.LogMessage("Congiguration changed successfully.", 0);
                this.Close();
            }
            else if (!bIsSavePathLoad)
                System.Windows.MessageBox.Show("Path cannot find..!\n Please select proper path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            else if (bIsSavePathLoad && ProjectKey != "P0")
                System.Windows.MessageBox.Show("Class file loading failed..!\nPlease choose proper Class type.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error);

            else if (ProjectKey == "P0")
            {
                System.Windows.MessageBox.Show("No Project has been selected..!", "Select Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                Utilities.LogMessage("No Project has been selected.", 0);
                this.Close();
            }
        }

        public bool SaveConfiguration()
        {
            try
            {
                bIsSavePathLoad = true;

                if(SelectedClassType != app.settings.ClassType || app.settings.ClassFilePath == "" || app.settings.ClassFilePath != txtClassPath.Text.Trim() || app.settings.Architecture != cmbArchitecture.Text){
                    app.settings.ClassFilePath = txtClassPath.Text.ToString();
                    app.settings.ReadClassFileConfig();
                    Utilities.LogMessage("Class file has been changed.", 0);
                }
                    
                app.settings.CurrentProject = ProjectKey;
                app.InitializeComboBox();

                if(app.settings.ClassType == EnumClassType.Any)
                {
                    chkRectangle.IsChecked = true;
                    chkPolygon.IsChecked = true;
                }
                else if(app.settings.ClassType == EnumClassType.Rectangle)
                {
                    chkRectangle.IsChecked = true;
                    chkPolygon.IsChecked = false;
                }
                else if (app.settings.ClassType == EnumClassType.Polyline)
                {
                    chkRectangle.IsChecked = false;
                    chkPolygon.IsChecked = true;
                }
                else if (app.settings.ClassType == EnumClassType.Segregation)
                {
                    chkRectangle.IsChecked = false;
                    chkPolygon.IsChecked = false;
                }
                chkRectangle_Checked(null, null);
                chkPloyline_Checked(null, null);

                txtModelPath.Text = txtModelPath.Text.Trim() == "" ? app.settings.StatsFilePath : txtModelPath.Text.Trim();
                if (txtExportCSVPath.Text.Trim() != "" && Directory.Exists(txtExportCSVPath.Text.Trim()))
                {
                    bIsSavePathLoad = true;
                    app.settings.CSVExportPath = txtExportCSVPath.Text.Trim();
                }
                else{
                    bIsSavePathLoad = false;
                    return false;
                }
                app.settings.ZoomLevel = Convert.ToDouble(sliderZoom.Value);

                if (chkValidationStats.IsChecked.Value && app.settings.blnValidationStat != chkValidationStats.IsChecked 
                    && app.settings.ImportFilePath != null && !app.settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) != ".csv"))
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    app.LoadViolatedDataFromCSV();
                    Mouse.OverrideCursor = null;
                    Utilities.LogMessage("Validation stats has been enabled.", 0);
                }
                app.settings.blnValidationStat = chkValidationStats.IsChecked.Value;
                app.settings.bIsValidatewithID = toggleValidateID.IsChecked.Value;
                app.ValidationStatVisibility = app.settings.blnValidationStat ? Visibility.Visible : Visibility.Collapsed;
                if (app.settings.ApplicationMode != SelectedMode)
                {
                    app.settings.ApplicationMode = SelectedMode;
                    app.CleanupLoadedData(true);
                    if (app.ImageMenuList.Count > 0)
                        app.ListBoxImages_SelectionChanged(null, null);
                    app.listBoxImages.Items.Refresh();
                    app.SetApplicationMenuControls();
                    app.settings.ImportFilePath = null;
                }
                else
                    app.SetApplicationMenuControls();

                if (app.settings.ModelPath != txtModelPath.Text.Trim())
                    app.bIsModelLoad = false;

                app.settings.ModelPath = txtModelPath.Text.Trim();
                app.settings.DefaultRadius = txtDefaultRad.Text.Trim() != "" ? Convert.ToInt32(txtDefaultRad.Text.Trim()) : 19;
                app.settings.WriteConfigSettings();
                if (app.settings.classCount > 0)
                    return true;
                else
                    return false; 
            }
            catch (System.Exception ex)
            {
                return false;
            }            
        }

        private void chkRectangle_Checked(object sender, RoutedEventArgs e)
        {
            app.IsEnableRectangle = chkRectangle.IsChecked == true ? true : false;
            app.IsEnableCircle = chkRectangle.IsChecked == true ? true : false;
            if (!app.IsEnableRectangle & app.SelectedShape == EnumSelectedShape.Rectangle)
            {
                app.SelectedShape = EnumSelectedShape.Null;
                app.SelectionRectangle.Background = Brushes.Transparent;
                app.btnQuickRect.Background = Brushes.Transparent;
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(app.SelectionRectangle), null);
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(app.btnQuickRect), null);
            }
        }

        private void chkCircle_Checked(object sender, RoutedEventArgs e)
        {
            app.IsEnableCircle = chkCircle.IsChecked == true ? true : false;
            if (!app.IsEnableCircle & app.SelectedShape == EnumSelectedShape.Circle)
            {
                app.SelectedShape = EnumSelectedShape.Null;
                app.SelectionCircle.Background = Brushes.Transparent;
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(app.SelectionCircle), null);
            }
        }
        
        private void chkPloyline_Checked(object sender, RoutedEventArgs e)
        {
            app.IsEnablePoly = chkPolygon.IsChecked == true ? true : false;
            if (!app.IsEnablePoly & app.SelectedShape == EnumSelectedShape.Polyline)
            {
                app.SelectedShape = EnumSelectedShape.Null;
                app.SelectionPoly.Background = Brushes.Transparent;
                app.btnQuickPoly.Background = Brushes.Transparent;
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(app.SelectionPoly), null);
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(app.btnQuickPoly), null);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void radClassType_Checked(object sender, RoutedEventArgs e)
        {
            //string strClassPath = GetClassPath(sender);
            string strProjectName = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Value;
            string strClassPath = GetClassPathfromName(strProjectName);

            if (!string.IsNullOrEmpty(strClassPath))
                txtClassPath.Text = strClassPath;
            else
            {
                string appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string[] classFiles = Directory.GetFiles(appPath, "*Genie*.ini", SearchOption.AllDirectories);
                if(classFiles.Length > 1)
                {
                    string[] temp = Directory.GetFiles(app.settings.StatsFilePath, "*Genie*.ini", SearchOption.AllDirectories);
                    for (int i = 0; i < temp.Length; i++)
                        System.IO.File.Delete(temp[i]);
                }

                int k = 0;
                while(k < classFiles.Length)
                {
                    string destFile = app.settings.StatsFilePath + Path.GetFileName(classFiles[k]);
                    System.IO.File.Copy(classFiles[k], destFile, true);
                    k++;
                }
                if(classFiles.Length > 0)
                    strClassPath = GetClassPath(sender);

                txtClassPath.Text = strClassPath;
            }

            if(sender == null && strClassPath != string.Empty)
            {
                IniFile iniLabel = new IniFile(strClassPath);
                string type = iniLabel.ReadValue("Annotation", "Type", "Any");

                if(type == "Any")
                {
                    radAny.IsChecked = true;
                    SelectedClassType = EnumClassType.Any;
                }   
                else if(type == "Segregation")
                {
                    radSegregation.IsChecked = true;
                    SelectedClassType = EnumClassType.Segregation;
                }
            }
            else if (sender != null)
            {
                string strShape = (sender as System.Windows.Controls.RadioButton).Content.ToString();
                strShape = strShape.Split('/').Length > 0 ? strShape.Split('/')[0] : strShape;
                SelectedClassType = (EnumClassType)Enum.Parse(typeof(EnumClassType), strShape, true);
            }
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

        private string GetClassPath(object sender)
        {
            string[] classIniFiles = Directory.GetFiles(app.settings.StatsFilePath, "*.ini", SearchOption.AllDirectories);
            for (int i = 0; i < classIniFiles.Length; i++)
            {
                IniFile iniLabel = new IniFile(classIniFiles[i]);
                if(sender != null)
                {
                    if (iniLabel.ReadValue("Annotation", "Type", "") == (sender as System.Windows.Controls.RadioButton).Content.ToString() &&
                    iniLabel.ReadValue("ClassInfo", "Project", "") == ProjectKey)
                    {
                        return classIniFiles[i];
                    }
                }
                else
                {
                    if (iniLabel.ReadValue("ClassInfo", "Project", "") == ProjectKey)
                        return classIniFiles[i];
                }
            }

            return null;
        }

        private void cmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProject.SelectedIndex == -1)
                return;
            ProjectKey = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Key;
            InitializeProjectControls();
            //if (ProjectKey == "P1"){
            //    radRecta.IsChecked = true;
            //    radClassType_Checked(radRecta, null);
            //}
            //else
            //    radClassType_Checked(null, null);
            string strProjectName = app.settings.dictProjectList.FirstOrDefault(item => item.Value == cmbProject.SelectedItem.ToString()).Value;
            string[] arrArchitecture = GetArchitectureArray(strProjectName);
            cmbArchitecture.ItemsSource = arrArchitecture;
            if(arrArchitecture.Length > 0)
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
                    if(strArch != "" & !listArchitecture.Contains(strArch))
                        listArchitecture.Add(strArch);
                }
            }
            return listArchitecture.ToArray();
        }

        private void btnModelPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = txtModelPath.Text;

            DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                txtModelPath.Text = folderDialog.SelectedPath;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();                
            }
        }

        private void AddProject_Click(object sender, MouseButtonEventArgs e)
        {
            string type = "New";
            ProjectConfigWindow projectWindow = new ProjectConfigWindow(app, type);
            projectWindow.Owner = this;
            projectWindow.ShowDialog();
        }

        private void cmbArchitecture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbArchitecture.SelectedIndex == -1)
                return;

            Architecture = cmbArchitecture.SelectedItem.ToString();
            if (cmbArchitecture.SelectedItem.ToString().Contains("Classification"))
            {
                radSegregation.IsChecked = true;
                radClassType_Checked(radSegregation, null);                
                
            }
            else if (cmbArchitecture.SelectedItem.ToString().Contains("Segmentation"))
            {
                radPoly.IsChecked = true;
                radClassType_Checked(radPoly, null);
            }
            else
            {
                radRecta.IsChecked = true;
                radClassType_Checked(radRecta, null);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
