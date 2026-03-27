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

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ClassIDAssignerWindow.xaml
    /// </summary>
    public partial class ClassIDAssignerWindow : Window
    {
        MainWindow app;
        public List<ClassStats> listClassStat = new List<ClassStats>();
        BackgroundWorker BGWorkerFormat;
        bool bIsSaveData = false;

        public ClassIDAssignerWindow(MainWindow app)
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
            listClassStat = app.GetClassFolderStatistics();
            lvClassList.ItemsSource = listClassStat;
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

        private void ButtonMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void txtSplit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if ((sender as TextBox).Text.Length > 3)
                e.Handled = true;

            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void txtModifyID_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ClassStats curClassStat = (sender as TextBox).DataContext as ClassStats;
            if (curClassStat == null)
                    return;

            if ((e.Key == Key.Enter || e.Key == Key.Down))
            {
                int index = listClassStat.IndexOf(curClassStat);
                if (index < lvClassList.Items.Count)
                {
                    lvClassList.SelectedIndex = index + 1;
                    ClassStats nextClassStat = listClassStat[lvClassList.SelectedIndex];
                    SetTextBoxFocus(nextClassStat);
                }
            }
            else if(e.Key == Key.Up)
            {
                int index = listClassStat.IndexOf(curClassStat);
                if (index > 0)
                {
                    lvClassList.SelectedIndex = index - 1;
                    ClassStats prevClassStat = listClassStat[lvClassList.SelectedIndex];
                    SetTextBoxFocus(prevClassStat);
                }
            }
        }

        private void SetTextBoxFocus(ClassStats setClassStat)
        {
            ItemContainerGenerator generator = lvClassList.ItemContainerGenerator;
            ListBoxItem selectedItem = (ListBoxItem)generator.ContainerFromItem(setClassStat);

            TextBox tbModifiedID = app.GetDescendantByType(selectedItem, typeof(TextBox), "txtModifiedID") as TextBox;
            if (tbModifiedID != null)
                tbModifiedID.Focus();
        }

        private void ButtonCSVExport_Click(object sender, MouseButtonEventArgs e)
        {
            if (!app.CheckFileAccessToFormat())
                return;

            if(!listClassStat.Exists(item => !string.IsNullOrEmpty(item.ModifiedID)))
            {
                MessageBox.Show("Modified ID not found", "Not Found", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            if(listClassStat.Where(item => !string.IsNullOrEmpty(item.ModifiedID)).GroupBy(temp => temp.ModifiedID).Where(a => a.Count() > 1).Count() > 0)
            {
                MessageBox.Show("Duplicate Modified ID found..! \nPlease make sure that ID's are identical.", "Duplicate ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            SaveModifiedIDtoDatasheet();
        }

        private void SaveModifiedIDtoDatasheet()
        {
            BGWorkerFormat = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            BGWorkerFormat.DoWork += bgwDowork_ModifyExportDatasheet;
            BGWorkerFormat.ProgressChanged += app.bgwProgressChange_Load;
            BGWorkerFormat.RunWorkerAsync();
            if(!bIsSaveData)
                app.OnWorkerMethodStartWithPercent_ProcessFile(this, "Processing Please wait...");
            else
                app.OnWorkerMethodStartWithPercent_ProcessFile(this, "Saving Changes..");
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            if (!listClassStat.Exists(item => !string.IsNullOrEmpty(item.ModifiedID))){
                this.Close();
                return;
            }
                

            MessageBoxResult result = System.Windows.MessageBox.Show("Save Changes?", "Save Datasheet", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No){
                this.Close();
                return;
            }

            if (listClassStat.Where(item => !string.IsNullOrEmpty(item.ModifiedID)).GroupBy(temp => temp.ModifiedID).Where(a => a.Count() > 1).Count() > 0)
            {
                MessageBox.Show("Duplicate Modified ID found..! \nPlease make sure that ID's are identical.", "Duplicate ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            bIsSaveData = true;
            app.bIsFormatFile = true;
            SaveModifiedIDtoDatasheet();
            this.Close();
        }

        private void bgwDowork_ModifyExportDatasheet(object sender, DoWorkEventArgs e)
        {
            if (BGWorkerFormat.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadExport = new Thread(ModifyAndExportDatasheet);
                threadExport.IsBackground = true;
                threadExport.Start();
            }
        }

        private void ModifyAndExportDatasheet()
        {
            try
            {
                string strDataPath = "";
                //if (bIsSaveData)
                //{
                //    strDataPath = app.settings.StatsFilePath + @"\Temp Files";
                //    if (System.IO.Directory.Exists(strDataPath))
                //        System.IO.Directory.Delete(strDataPath);
                //}
                //else
                strDataPath = app.settings.CSVExportPath + @"\Output Data\IDAssigner Data";

                if (!System.IO.Directory.Exists(strDataPath))
                    System.IO.Directory.CreateDirectory(strDataPath);

                foreach (var curItem in listClassStat)
                    if (!string.IsNullOrEmpty(curItem.ModifiedID))
                        app.ListModifiedClass.Where(temp => temp.ModifiedClassName == curItem.AliasName).ToList().ForEach(id => id.ModifiedID = curItem.ModifiedID);

                StringBuilder sb = new StringBuilder();
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                for (int index = 0; index < app.ListDatasheetImportData.Count; index++)
                {
                    string temp = app.ListDatasheetImportData.Count == 1 ? "" : (index + 1).ToString("(0)");
                    string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + temp + ".csv");

                    string[] arrColumnHeader = app.DictColHeaders[app.ListDatasheetImportData[index].DatasheetName];

                    List<string> listCSVLines = ReAssignClassID(app.ListDatasheetImportData[index].ListImportData);
                    if (!bIsSaveData)
                    {
                        sb.Clear();
                        sb.AppendLine(string.Join(",", "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                        string colHeader = string.Join(",", arrColumnHeader);
                        File.WriteAllText(strCSVSavePath, sb.AppendLine(colHeader).ToString());
                        File.AppendAllLines(strCSVSavePath, listCSVLines);
                    }                        
                }

                app.OnWorkerMethodComplete("complete");
                if (!bIsSaveData)
                    System.Windows.MessageBox.Show("Datasheet Exported Successfully to Path below \n" + strDataPath, "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                app.OnWorkerMethodComplete("complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (Exception ex)
            {
                app.OnWorkerMethodComplete("complete");
                if (!bIsSaveData)
                    MessageBox.Show("Something went wrong..!Data Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                else
                    MessageBox.Show("Something went wrong..!Changes could not save..!", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);

                Utilities.LogMessage("ClassIDAssignerWindow::ModifyAndExportDatasheet: " + ex.Message, 0);
            }
        }

        private List<string> ReAssignClassID(List<string[]> listCSVLines)
        {
            Dispatcher.Invoke(() => app.progressBar.pbStatus.Maximum = listCSVLines.Count);
            for (int lines = 0; lines < listCSVLines.Count; lines++)
            {
                string[] lineSplit = listCSVLines[lines];
                if (!app.IsValidCSVLine(lineSplit.Select(temp => Regex.Replace(temp, @"[""{}]", "")).ToArray()) || app.IsColHeaderLine(lineSplit.Select(temp => Regex.Replace(temp, @"[""{}]", "")).ToArray())){
                    Dispatcher.Invoke(() => app.progressBar.pbStatus.Value = lines);
                    continue;
                }

                string ClassName = Regex.Match(Regex.Replace(lineSplit[3], @"[""{}]", ""), @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim().ToUpper();
                ClassStats curClassStat = listClassStat.FirstOrDefault(temp => !string.IsNullOrEmpty(temp.ModifiedID) && temp.AliasName.ToUpper() == ClassName);
                if (curClassStat != null){
                    string strRegion = "{\"class id\":\"" + curClassStat.ModifiedID + "\", \"class name\":\"" + ClassName + "\"}";
                    lineSplit[3] = "\"" + strRegion.Replace("\"", "\"\"") + "\"";
                }
                Dispatcher.Invoke(() => app.progressBar.pbStatus.Value = lines);
            }

            return listCSVLines.Select(temp => string.Join(",", temp)).ToList();
        }

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
