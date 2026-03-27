using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ClassFolderStats.xaml
    /// </summary>
    public partial class ClassFolderStats : Window, INotifyPropertyChanged
    {
        MainWindow app;
        public event PropertyChangedEventHandler PropertyChanged;
        public List<ClassStats> listClassStat = new List<ClassStats>();
        string[] arrSelDatasheet;

        public ClassFolderStats(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            InitializeControls();
            DataContext = this;
            this.SizeChanged += ClassFolderStats_SizeChanged;  
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }

        private void ClassFolderStats_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NotifyPropertyChanged("SetColumnWidthTotalCount");
            this.SizeChanged -= ClassFolderStats_SizeChanged;
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
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
            //listClassStat = app.GetClassFolderStatistics();
            LoadClassFolderStats();

            lvClassList.ItemsSource = listClassStat;
            tbClassCountHeader.Text = app.settings.ClassType == EnumClassType.Segregation ? "Total Segregated Images : " : "Total Labelled Regions : \n(Excluding Correction Images)";
            tbClassCount.Text = listClassStat.Sum(item => item.Count).ToString();
            tbDatasheetName.Text = app.settings.ImportFilePath != null && app.settings.ImportFilePath.Length > 0 ? "All Datasheet" : "No Datasheet";
        }

        List<string> fileList = new List<string>();
        public List<string> LoadedFileList
        {
            get
            {
                if(app.settings.ImportFilePath == null || app.settings.ImportFilePath.Length == 0)
                    fileList.Add("No DataSheet Loaded");
                else
                    fileList = app.settings.ImportFilePath.ToList();

                return fileList;
            }
        }

        private void LoadClassFolderStats()
        {
            string[] arrayValue = app.settings.dictEVSupervisorClass.Values.ToArray();
            listClassStat = new List<ClassStats>();
            var tempImageMenuList = app.ImageMenuList.Where(item => item.ImageBox.ListImageClass.Count > 0 && item.MenuItemBrush != app.ImageMenuBrushes[2]).ToList();
            foreach (var tempItem in app.settings.dictEVSupervisorClass)
            {
                string strAlias = tempItem.Value.Split('(', ')').Length > 1 ? tempItem.Value.Split('(', ')')[1] : "";
                var curClassList = tempImageMenuList.SelectMany(item => item.ImageBox.ListImageClass.Where(s => s.ClassAlias.ToUpper() == strAlias.ToUpper())).ToList();
                ClassStats curClassStats = new ClassStats();
                listClassStat.Add(curClassStats);

                curClassStats.ClassName = tempItem.Value.Split('(', ')').Length > 0 ? tempItem.Value.Split('(', ')')[0] : "";
                curClassStats.AliasName = strAlias;
                curClassStats.ClassID = tempItem.Key.ToString();

                var tempModifiedClass = app.ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == strAlias.ToUpper());
                curClassStats.CurrentClassID = tempModifiedClass != null ? tempModifiedClass.ModifiedID : "";
                curClassStats.Count = curClassList.Count;
                curClassStats.SingleSpotCount = 0;
                curClassStats.PhaseContrastCount = 0;
                curClassStats.ClassType = app.settings.ListFailClass.Contains(strAlias) ? "Fail" : "Pass";
            }
            Utilities.LogMessage("Class folder stats loaded.");
        }

        private void RefreshClassFolderStats(string strDatasheetName)
        {
            string[] arrayValue = app.settings.dictEVSupervisorClass.Values.ToArray();
            listClassStat = new List<ClassStats>();
            foreach (var tempItem in app.settings.dictEVSupervisorClass)
            {
                string strAlias = tempItem.Value.Split('(', ')').Length > 1 ? tempItem.Value.Split('(', ')')[1] : "";
                var curFolderstat = app.ListClassFolderStat.FirstOrDefault(item => item.ClassAliasName.ToUpper() == strAlias.ToUpper() && item.ImportDatasheetName == strDatasheetName);
                ClassStats curClassStats = new ClassStats();
                listClassStat.Add(curClassStats);

                curClassStats.ClassName = tempItem.Value.Split('(', ')').Length > 0 ? tempItem.Value.Split('(', ')')[0] : "";
                curClassStats.AliasName = strAlias;
                curClassStats.ClassID = tempItem.Key.ToString();

                var tempModifiedClass = app.ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == strAlias.ToUpper());
                curClassStats.CurrentClassID = tempModifiedClass != null ? tempModifiedClass.ModifiedID : "";
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

            string[] arrayAlias = app.settings.dictEVSupervisorClass.Values.Select(temp => temp.Split('(', ')').Length > 1 ? temp.Split('(', ')')[1].ToUpper() : "").ToArray();
            var listClass = app.ListClassFolderStat.Where(item => !arrayAlias.Contains(item.ClassAliasName.ToUpper()) && item.ImportDatasheetName == strDatasheetName).ToList();

            foreach (var curClass in listClass)
            {
                var tempModifiedClass = app.ListModifiedClass.FirstOrDefault(temp => temp.ModifiedClassName.ToUpper() == curClass.ClassAliasName.ToUpper());
                listClassStat.Add(new ClassStats
                {
                    ClassName = "Unknown Class",
                    AliasName = curClass.ClassAliasName,
                    Count = curClass.ClassCount,
                    SingleSpotCount = curClass.SingleSpotCount,
                    PhaseContrastCount = curClass.PhaseContrastCount,
                    ClassID = "",
                    CurrentClassID = tempModifiedClass != null ? tempModifiedClass.ModifiedID : ""
                });
            }
        }

        public double SetColumnWidthField
        {
            get
            {
                //if (app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV"))
                //    return 150;
                //else
                return 0;
            }            
        }

        public double SetColumnWidthTotalCount
        {
            get
            {
                //if (app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV"))
                //    return 200;
                //else
                return (this.ActualWidth - 450); 
            }
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ButtonMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ButtonCSVExport_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listClassStat == null || listClassStat.Count == 0)
                {
                    MessageBox.Show("No Classes Found..!", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Folder Stats";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.InitialDirectory = strDataPath;
                saveFileDialog.Filter = "csv file|*.csv";
                saveFileDialog.FileName = "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv";

                System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    strCSVSavePath = saveFileDialog.FileName;
                }
                else
                    return;

                string seperator = ",";
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Join(seperator, "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                //if (!app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV"))
                //    sb.AppendLine("Class Name,Alias Name,Total Count");
                //else
                //    sb.AppendLine("Class Name,Alias Name,Single Spot Count,Phase Contrast Count,Total Count");
                sb.AppendLine("Class Name,Alias Name,Class ID,Type,Total Count");

                for (int cnt = 0; cnt < listClassStat.Count; cnt++)
                {
                    ClassStats curClassStats = listClassStat[cnt] as ClassStats;
                    //if (!app.settings.dictProjectList[app.settings.CurrentProject].Contains("LS3 BV"))
                    //    sb.AppendLine(string.Join(seperator, curClassStats.ClassName, curClassStats.AliasName, curClassStats.Count));
                    //else
                    //    sb.AppendLine(string.Join(seperator, curClassStats.ClassName, curClassStats.AliasName, curClassStats.SingleSpotCount, curClassStats.PhaseContrastCount, curClassStats.Count));
                    sb.AppendLine(string.Join(seperator, curClassStats.ClassName, curClassStats.AliasName, curClassStats.ClassID, curClassStats.ClassType, curClassStats.Count));
                }

                File.WriteAllText(strCSVSavePath, sb.ToString());
                MessageBox.Show("CSV Exported Successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Class folder stats csv Exported.");
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, 
                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void listLoadedFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listLoadedFile.ItemsSource == null || listLoadedFile.Items.Count == 1)
                return;

            arrSelDatasheet = listLoadedFile.SelectedItems.Cast<string>().ToArray();
            listClassStat = app.GetClassFolderStatistics(arrSelDatasheet);
            lvClassList.ItemsSource = listClassStat;
            tbClassCount.Text = listClassStat.Sum(item => item.Count).ToString();
            tbDatasheetName.Text = arrSelDatasheet == null || arrSelDatasheet.Length == 0 ? "All Datasheet" : string.Join(",  ", arrSelDatasheet);
            //NotifyPropertyChanged("SetColumnWidthTotalCount");
        }
    }

    public class ClassStats
    {
        public string ClassName { get; set; }

        public string AliasName { get; set; }

        public string ClassID{ get; set; }

        public string ModifiedID { get; set; }

        public int Count { get; set; }

        public int SingleSpotCount { get; set; }

        public int PhaseContrastCount { get; set; }

        public string CurrentClassID { get; set;}

        public string ClassType { get; set; }

        public Brush FieldBackColor
        {
            get
            {
                if (Count > 0 && ClassName != "Unknown Class")
                    return Brushes.White;
                else if (Count > 0 && ClassName == "Unknown Class")
                    return Brushes.GreenYellow;
                else
                    return Brushes.HotPink;
            }

            set { }
        }
    }
}
