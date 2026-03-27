using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ImageValidationWindow.xaml
    /// </summary>
    public partial class ImageValidationWindow : Window
    {
        MainWindow app;
        public BackgroundWorker bgWorkerValidate;
        bool bIsDropExport = false;

        public ImageValidationWindow(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            DataContext = this;
            //LoadDataGridValidationReport();
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

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ButtonMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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

        public void LoadDataGridValidationReport()
        {
            for (int i = 0; i < app.settings.ImportFilePath.Length; i++)
            {
                if (app.settings.CheckFileAccess(app.settings.ImportFilePath[i]))
                {
                    app.OnWorkerMethodComplete("complete");
                    System.Windows.MessageBox.Show("Some CSV files cannot be accessible\nMake sure the file is not accessed by other application.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            Dispatcher.Invoke(() =>
            {
                dgDatasheet.Columns.Clear();
                dgDatasheet.ItemsSource = null;
            });

            DataTable dtValidReport = new DataTable();
            dtValidReport.Columns.Add("Image name");
            dtValidReport.Columns.Add("Row Number");
            DataRow drGridRow;
            long tempTotal = 0;

            for (int index = 0; index < app.settings.ImportFilePath.Length; index++)
            {
                List<string> listCSVLines = File.ReadAllLines(app.settings.ImportFilePath[index]).ToList();
                string[] split = listCSVLines[0].Split(',');
                if (split.Contains("file_size") && split.Length > 6)
                    listCSVLines = app.PrevFormatListValues(listCSVLines);
                else
                    listCSVLines = app.CurrentFormatListValues(listCSVLines);

                int skipCount = File.ReadAllLines(app.settings.ImportFilePath[index]).ToList().Count - listCSVLines.Count;
                drGridRow = dtValidReport.NewRow();
                drGridRow[0] = app.settings.ImportFilePath[index].ToString();
                dtValidReport.Rows.Add(drGridRow.ItemArray);

                int cnt = 0;
                for (int row = 0; row < listCSVLines.Count; row++)
                {
                    if (app.IsHeaderLine(listCSVLines[row])) 
                        continue;

                    string strImage = listCSVLines[row].Split(',')[0];
                    if (!app.ImageMenuList.ToList().Exists(item => item.ImageName == strImage))
                    {
                        cnt++;
                        drGridRow = dtValidReport.NewRow();
                        drGridRow[0] = cnt + ".  " + strImage;
                        drGridRow[1] = (row + skipCount + 1).ToString();
                        dtValidReport.Rows.Add(drGridRow.ItemArray);
                    }
                }
                if (cnt == 0)
                {
                    drGridRow = dtValidReport.NewRow();
                    drGridRow[0] = "No missing images";
                    dtValidReport.Rows.Add(drGridRow.ItemArray);
                }
                tempTotal += cnt;

                if (index == app.settings.ImportFilePath.Length - 1)
                    continue;
                drGridRow = dtValidReport.NewRow();
                drGridRow[0] = "";
                dtValidReport.Rows.Add(drGridRow.ItemArray);
            }

            Dispatcher.Invoke(() =>
            {
                dgDatasheet.ItemsSource = dtValidReport.DefaultView;
                dgDatasheet.Items.Refresh();
                SetColumnHeaderWidth();
                lblTotalCount.Content = tempTotal.ToString();
            });
            app.OnWorkerMethodComplete("complete");
            Utilities.LogMessage("Image validation report generated.");
        }

        private void SetColumnHeaderWidth()
        {
            Style colImagename = new Style();
            colImagename.Setters.Add(new Setter(DataGridCell.HorizontalContentAlignmentProperty, HorizontalContentAlignment = HorizontalAlignment.Left));
            dgDatasheet.Columns[0].CellStyle = colImagename;

            foreach (var oColumn in dgDatasheet.Columns)
            {
                DataGridTextColumn curColumn = oColumn as DataGridTextColumn;
                if (oColumn.Header.ToString() == "Image name")
                {
                    curColumn.Width = new DataGridLength(0.8, DataGridLengthUnitType.Star);
                }
                else
                {
                    curColumn.Width = new DataGridLength(0.2, DataGridLengthUnitType.Star);
                }
            }
        }

        private void ButtonValidate_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Button).Name == "btnCSVDropExport")
                bIsDropExport = true;
            else
                bIsDropExport = false;

            bgWorkerValidate = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            if((sender as Button).Name == "btnValidate")
                bgWorkerValidate.DoWork += bgwDowork_ImageValidate;
            else
                bgWorkerValidate.DoWork += bgwDowork_ExportData;
            bgWorkerValidate.ProgressChanged += app.bgwProgressChange_Load;
            bgWorkerValidate.RunWorkerAsync();
            app.OnWorkerMethodStart_LoadFile(this);
        }

        private void bgwDowork_ExportData(object sender, DoWorkEventArgs e)
        {
            if (bgWorkerValidate.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                if (dgDatasheet.Items.Count == 0 || dgDatasheet.Items == null)
                {
                    app.OnWorkerMethodComplete("complete");
                    MessageBox.Show("Please Validate before Export..!", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Thread threadExport = null;
                if (bIsDropExport)
                    threadExport = new Thread(DropandExportValidatedCSV);

                else
                    threadExport = new Thread(ExportValidateDatatoCSV);

                if (threadExport != null)
                {
                    threadExport.IsBackground = true;
                    threadExport.Start();
                }
            }
        }

        private void bgwDowork_ImageValidate(object sender, DoWorkEventArgs e)
        {
            if (bgWorkerValidate.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                Thread threadValidate = new Thread(LoadDataGridValidationReport);
                threadValidate.IsBackground = true;
                threadValidate.Priority = ThreadPriority.Lowest;
                threadValidate.Start();
            }
        }

        private void ExportValidateDatatoCSV()
        {
            try
            {              
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Image validated";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";

                string seperator = ",";
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Join(seperator, "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                for (int index = 0; index < app.settings.ImportFilePath.Length; index++)
                {
                    List<string> listCSVLines = File.ReadAllLines(app.settings.ImportFilePath[index]).ToList();
                    string[] split = listCSVLines[0].Split(',');
                    if (split.Contains("file_size") && split.Length > 6)
                        listCSVLines = app.PrevFormatListValues(listCSVLines);
                    else
                        listCSVLines = app.CurrentFormatListValues(listCSVLines);

                    int skipCount = File.ReadAllLines(app.settings.ImportFilePath[index]).ToList().Count - listCSVLines.Count;
                    sb.AppendLine(string.Join(seperator, "CSV Datasheet : " + app.settings.ImportFilePath[index].ToString()));
                    sb.AppendLine("filename,row_number,region_shape_attributes,region_attributes,to_be_corrected");
                    int cnt = 0;
                    for (int row = 0; row < listCSVLines.Count; row++)
                    {
                        if (app.IsHeaderLine(listCSVLines[row]))
                            continue;

                        string[] strSplit = Regex.Split(listCSVLines[row], @"(?<!,[^[]+\{[^}]+),");
                        if (!app.ImageMenuList.ToList().Exists(item => item.ImageName == strSplit[0]))
                        {
                            cnt++;
                            sb.AppendLine(string.Join(seperator, strSplit[0], (row + skipCount + 1).ToString(), strSplit[2], strSplit[3]));
                        }
                    }
                    if (cnt == 0)
                        sb.AppendLine(string.Join(seperator, "","","No missing images"));

                    if (index == app.settings.ImportFilePath.Length - 1)
                        continue;
                    sb.AppendLine(string.Join(seperator, " "));
                }

                File.WriteAllText(strCSVSavePath, sb.ToString());
                app.OnWorkerMethodComplete("complete");
                System.Windows.MessageBox.Show("Validated CSV Exported Successfully to Path below \n" + strDataPath, "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Validated CSV Exported to path " + strDataPath);
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
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ImageValidation::ExportFile_Click: " + ex.Message, 0);
            }
        }

        private void DropandExportValidatedCSV()
        {
            try
            {
                string strDataPath = app.settings.CSVExportPath + @"\Output Data\Image validated\Drop and Validated";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                StringBuilder sb = new StringBuilder();
                string strProjectname = app.settings.dictProjectList.ContainsKey(app.settings.CurrentProject) ? app.settings.dictProjectList[app.settings.CurrentProject] : "";
                string seperator = ",";
                List<string> tempImageList = app.ImageMenuList.Select(a => a.ImageName).ToList();
                for (int index = 0; index < app.settings.ImportFilePath.Length; index++)
                {
                    string temp = app.settings.ImportFilePath.Length == 1 ? "" : (index + 1).ToString("(0)");
                    string strCSVSavePath = System.IO.Path.Combine(strDataPath, "data_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + temp + ".csv");

                    List<string> listCSVLines = File.ReadAllLines(app.settings.ImportFilePath[index]).ToList();
                    string[] arrColumnHeader = app.GetColumnHeaderNames(listCSVLines);

                    if (listCSVLines[0].Split(',').Contains("file_size") && listCSVLines[0].Split(',').Length > 6)
                        listCSVLines = app.PrevFormatListValues(listCSVLines);
                    else
                        listCSVLines = app.CurrentFormatListValues(listCSVLines);

                    listCSVLines = listCSVLines.Where(item => tempImageList.Contains(item.Split(',')[0].ToString())).ToList();
                    sb.Clear();
                    sb.AppendLine(string.Join(seperator, "Date : " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss tt"), " Project : " + strProjectname));
                    string colHeader = string.Join(",", arrColumnHeader);
                    File.WriteAllText(strCSVSavePath, sb.AppendLine(colHeader).ToString());
                    if(listCSVLines.Count > 0)
                        File.AppendAllLines(strCSVSavePath, listCSVLines);
                    else
                    {
                        sb.Clear();
                        sb.AppendLine(string.Join(seperator, "No missing images"));
                        File.AppendAllText(strCSVSavePath, sb.ToString());
                    }
                }

                app.OnWorkerMethodComplete("complete");
                System.Windows.MessageBox.Show("Dropped CSV Exported Successfully to Path below \n" + strDataPath, "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("Dropped CSV Exported to path " + strDataPath);
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
                MessageBox.Show("Something went wrong..!\nData Could not Export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("ImageValidation::DropandExportFile_Click: " + ex.Message, 0);
            }
        }
    }
}
