using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for FormatXMLWindow.xaml
    /// </summary>
    public partial class FormatXMLWindow : Window
    {
        MainWindow app;

        public FormatXMLWindow(MainWindow app)
        {
            InitializeComponent();
            this.app = app;
            txtXMLImagePath.Text = "";
            chkOverrideValid.IsChecked = false;
            DataContext = this;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private Visibility isVisibleCheckbox = Visibility.Visible;
        public Visibility IsVisibleCheckbox
        {
            get
            {
                if (app.settings.ImportFilePath != null && app.settings.ImportFilePath.Length > 0 && app.settings.ImportFilePath.ToList().Exists(s => System.IO.Path.GetExtension(s) == ".csv"))
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }

            set
            {
                isVisibleCheckbox = value;
            }
        }

        private void btnExportCSVPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = txtXMLImagePath.Text.Trim() == "" ? app.settings.CSVExportPath : txtXMLImagePath.Text;

            DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                txtXMLImagePath.Text = folderDialog.SelectedPath;
        }

        private void btnXMLExport_Click(object sender, RoutedEventArgs e)
        {
            app.DefaultPath = txtXMLImagePath.Text.Trim();
            app.DefaultSize = radDef.IsChecked.Value ? "1700" : "0";
            app.bIsOverrideValid = chkOverrideValid.IsChecked.Value;
            app.bIsAppendReview = chkAppendReview.IsChecked.Value;
            if (app.DefaultPath == "")
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("No path selected..! \nDo you want to continue without default path?",
                   "Format XML", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            if (app.bIsOverrideValid && !app.CheckFileAccessToFormat())
                return;

            app.bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            app.bgWorker.DoWork += app.bgwDowork_ExportasXML;
            app.bgWorker.ProgressChanged += app.bgwProgressChange_Load;
            app.bgWorker.RunWorkerAsync();
            app.OnWorkerMethodStart_LoadFile(app);
            this.Close();
        }

        private void btnClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
