using System;
using System.Collections.Generic;
using System.Data;
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
    /// Interaction logic for ValidationReportWindow.xaml
    /// </summary>
    public partial class ValidationReportWindow : Window
    {
        MainWindow app;
        InterfacePDF pdfExport;
        public bool bLoadSuccess = true;

        public ValidationReportWindow(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            pdfExport = new InterfacePDF();
            InitializeControls();
            this.btnValidate.Click += new System.Windows.RoutedEventHandler(this.btnValidate_Click);
            //LoadDataGridVioLationReport();
            DataContext = this;
            //LoadViolatedDataFromCSV();
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
            foreach (CheckBox chkboxItem in panelChkBox.Children)
                chkboxItem.IsChecked = true;

            chkShapeAttribute.Visibility = app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline ? Visibility.Visible : Visibility.Collapsed;
            chkShapeAttribute.IsChecked = app.settings.ClassType == EnumClassType.Rectangle || app.settings.ClassType == EnumClassType.Polyline ? true : false;
        }

        private void ButtonSaveClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ButtonMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
       
        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDataGridVioLationReport();
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDataGridVioLationReport()
        {
            dgDatasheet.Columns.Clear();
            dgDatasheet.ItemsSource = null;
            if (app.ListDataViolation.Count == 0)
            {
                DataGridTextColumn textColumn1 = new DataGridTextColumn();
                textColumn1.Header = "No Violation Found";
                textColumn1.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                dgDatasheet.Columns.Add(textColumn1);
                return;
            }

            DataTable dtValidReport = new DataTable();
            dtValidReport.Columns.Add("Image name");
            dtValidReport.Columns.Add("Row Number");
            DataRow drGridRow;

            foreach (CheckBox chkboxItem in panelChkBox.Children)
                dtValidReport.Columns.Add(chkboxItem.Content.ToString());

            for (int index = 0; index < app.settings.ImportFilePath.Length; index++)
            {
                List<DataViolation> listItems = app.ListDataViolation.Where(item => item.ImagePathName == app.settings.ImportFilePath[index]).ToList().OrderBy(i => i.ViolatedRow).ToList();
                if (listItems.Count > 0)
                {
                    drGridRow = dtValidReport.NewRow();
                    drGridRow[0] = app.settings.ImportFilePath[index].ToString();
                    dtValidReport.Rows.Add(drGridRow.ItemArray);

                    for (int i = 0; i < listItems.Count; i++)
                    {
                        drGridRow = dtValidReport.NewRow();
                        drGridRow[0] = i + 1 + ".  " + listItems[i].ImageFileName.ToString();
                        drGridRow[1] = listItems[i].ViolatedRow.ToString();
                        drGridRow[2] = listItems[i].FilenameViolated ? "Violated" : "Ok";
                        drGridRow[3] = listItems[i].RegionCountViolated ? "Violated" : "Ok";
                        drGridRow[4] = listItems[i].ShapeViolated ? "Violated" : "Ok";
                        drGridRow[5] = listItems[i].RegionClassViolated ? "Violated" : "Ok";
                        dtValidReport.Rows.Add(drGridRow.ItemArray);
                    }
                    if (index == app.settings.ImportFilePath.Length - 1)
                        continue;
                    drGridRow = dtValidReport.NewRow();
                    drGridRow[0] = "";
                    dtValidReport.Rows.Add(drGridRow.ItemArray);
                }
            }

            dgDatasheet.ItemsSource = dtValidReport.DefaultView;
            dgDatasheet.Items.Refresh();
            SetColumnHeaderWidth();
            Utilities.LogMessage("Violation in CSV files report loaded.");
        }

        private void SetColumnHeaderWidth()
        {
            List<string> listColumn = new List<string>();
            foreach (CheckBox chkboxItem in panelChkBox.Children)
            {
                if (chkboxItem.IsChecked == true)
                    listColumn.Add(chkboxItem.Content.ToString());
            }
            Style colImagename = new Style();
            colImagename.Setters.Add(new Setter(DataGridCell.HorizontalContentAlignmentProperty, HorizontalContentAlignment = HorizontalAlignment.Left));
            dgDatasheet.Columns[0].CellStyle = colImagename;

            foreach (var oColumn in dgDatasheet.Columns)
            {
                DataGridTextColumn curColumn = oColumn as DataGridTextColumn;
                if (oColumn.Header.ToString() == "Image name")
                {
                    curColumn.Width = new DataGridLength(0.6, DataGridLengthUnitType.Star);
                }
                else
                {
                    curColumn.Width = new DataGridLength(0.2, DataGridLengthUnitType.Star);
                    curColumn.Visibility = listColumn.Contains(oColumn.Header) || oColumn.Header.ToString() == "Row Number" ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        private void btnPdfExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDatasheet.Columns.Count > 0)
                {
                    popUpWait.IsOpen = true;
                    bool bSuccess = ExportViolationReportToPDF();
                    popUpWait.IsOpen = false;

                    if (bSuccess)
                    {
                        Utilities.LogMessage("Violation stats report exported as pdf.");
                        MessageBox.Show("PDF file Exported successfully", "Data Export", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
                else
                    MessageBox.Show("Click on Validate to check Violations Data", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                popUpWait.IsOpen = false;
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK, MessageBoxImage.Error, 
                    MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("The specified Output Data path, file name, or both are too long.");
            }
        }

        private bool ExportViolationReportToPDF()
        {
            List<string> listColumn = new List<string>();
            foreach (CheckBox chkboxItem in panelChkBox.Children)
            {
                if (chkboxItem.IsChecked == true)
                    listColumn.Add(chkboxItem.Content.ToString().Replace(" ", "_").ToLower());
            }

            string strPdfPath = app.settings.CSVExportPath + @"\Output Data\Supervisor Report";
            if (!Directory.Exists(strPdfPath))
                Directory.CreateDirectory(strPdfPath);

            string strPDFSavePath = System.IO.Path.Combine(strPdfPath, "Supervisor_Report_" + DateTime.Now.ToString("ddMMyyyy_HHmmss"));
            popUpWait.IsOpen = false;
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.InitialDirectory = strPdfPath;
            saveFileDialog.Filter = "pdf file|*.pdf";
            saveFileDialog.FileName = "Supervisor_Report_" + DateTime.Now.ToString("ddMMyyyy_HHmmss");

            System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                strPDFSavePath = saveFileDialog.FileName;
            }
            else
                return false;

            popUpWait.IsOpen = true;
            pdfExport.InitSettings(20, 20, 15, 20, strPDFSavePath);
            pdfExport.InitPdf();
            string strHeading = "Genie Supervisor";
            pdfExport.AppendTextHeading(strHeading, true);
            strHeading = "Labelled Data Validation Report";
            pdfExport.AppendTextHeading(strHeading, false);
            string strData;
            string[] data = new string[2];
            data[0] = "Date   : " + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
            data[1] = "Project : " + app.settings.dictProjectList[app.settings.CurrentProject];
            pdfExport.AppendDateString(data);
            pdfExport.AppendLine();

            if (dgDatasheet.Items.Count == 0)
            {
                //strData = "Status  : No violation found";
                //pdfExport.AppendText(strData);                
                string[] strNoDataHeader = new string[listColumn.Count];
                float[] Blankwidths = new float[listColumn.Count];
                for (int i = 0; i < listColumn.Count; i++)
                {
                    strNoDataHeader[i] = listColumn[i].ToString();
                    Blankwidths[i] = 1f;
                }
                for (int cnt = 0; cnt < app.settings.ImportFilePath.Length; cnt++)
                {
                    strData = "CSV File name: " + app.settings.ImportFilePath[cnt].ToString();
                    pdfExport.AppendText(" ");
                    pdfExport.AppendText(strData);
                    pdfExport.AppendTableHeader(strNoDataHeader, Blankwidths);
                    strData = "No Violations Found";
                    pdfExport.AppendBlankText(strData);
                }
            }
            else
            {
                //strData = "Status : Violation found";
                //pdfExport.AppendText(strData);
                string[] strTableHeader = new string[listColumn.Count + 2];
                float[] widths = new float[listColumn.Count + 2];
                strTableHeader[0] = "Image name";
                widths[0] = 4f;
                strTableHeader[1] = "Row Number";
                widths[1] = 1f;
                for (int i = 0; i < listColumn.Count; i++)
                {
                    strTableHeader[i + 2] = listColumn[i].ToString();
                    widths[i + 2] = 1.2f;
                }

                List<string> listTableContent;
                for (int cnt = 0; cnt < app.settings.ImportFilePath.Length; cnt++)
                {
                    List<DataViolation> listItems = app.ListDataViolation.Where(item => item.ImagePathName == app.settings.ImportFilePath[cnt]).ToList().OrderBy(i => i.ViolatedRow).ToList();
                    if (listItems.Count > 0)
                    {
                        strData = "CSV File name: " + app.settings.ImportFilePath[cnt].ToString();
                        pdfExport.AppendText(" ");
                        pdfExport.AppendText(strData);
                        pdfExport.AppendTableHeader(strTableHeader, widths);
                        for (int index = 0; index < listItems.Count; index++)
                        {
                            listTableContent = new List<string>();
                            listTableContent.Add(listItems[index].ImageFileName.ToString());
                            listTableContent.Add(listItems[index].ViolatedRow.ToString());

                            if (chkFilename.IsChecked == true)
                                listTableContent.Add(listItems[index].FilenameViolated ? "Violated" : "Ok");
                            if (chkRegionCount.IsChecked == true)
                                listTableContent.Add(listItems[index].RegionCountViolated ? "Violated" : "Ok");
                            if (chkShapeAttribute.IsChecked == true)
                                listTableContent.Add(listItems[index].ShapeViolated ? "Violated" : "Ok");
                            if (chkClassAttribute.IsChecked == true)
                                listTableContent.Add(listItems[index].RegionClassViolated ? "Violated" : "Ok");

                            pdfExport.AppendTableRows(listTableContent);
                        }
                    }
                }
            }
            pdfExport.CloseFile();

            return true;
        }
    }
}
