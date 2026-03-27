using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ClassSelectionWindow.xaml
    /// </summary>
    public partial class ClassSelectionWindow : Window
    {
        MainWindow app;
        string OperationType = "";
        List<ClassFormat> listClassFormat;

        public ClassSelectionWindow(MainWindow app, string InputType)
        {
            InitializeComponent();
            this.app = app;
            this.OperationType = InputType;
            InitialiZeControls();
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

        private void InitialiZeControls()
        {
            listClassFormat = new List<ClassFormat>();
            listClassFormat = app.GetClassFormatList();
            ListClassView.ItemsSource = listClassFormat;

            chkInclude.Content = app.settings.ClassType != EnumClassType.Segregation ? "Retrieve Unlabelled images also" : "Retrieve Unsegregated images also";
            spTop.Visibility = (OperationType == "ROI") ? Visibility.Collapsed : Visibility.Visible;
            lblHeading.Content = (OperationType == "ROI") ? "Export ROIs" : (OperationType == "Copy") ? "Copy Images" : "Move Images";
        }

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            bool bIsMoveImagesToRetrieve = true;
            var listFilterClass = listClassFormat.Where(item => item.IsClassEnable == true).Select(s => s.Alias.ToUpper()).ToList();
            var listNotEnableClass = listClassFormat.Where(item => item.ClassCount == 0).Select(s => s.Alias).Where(temp => listFilterClass.Contains(temp.ToUpper())).ToList();
            if ((OperationType == "ROI" && listFilterClass.Count == 0) || (OperationType != "ROI" && listFilterClass.Count == 0 && !chkInclude.IsChecked.Value))
            {
                MessageBox.Show("No classes selected..! Please select minimum one class to proceed..", "Warning..!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (listNotEnableClass.Count > 0){
                MessageBoxResult result = MessageBox.Show("\"" + string.Join("\", \"", listNotEnableClass) + "\" selected class not found in datasheet.. \nDo you want to skip that class and continue?", "Warning..!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if(result == MessageBoxResult.No)
                    return;
            }

            if (OperationType == "Move"){
                MessageBoxResult result = System.Windows.MessageBox.Show("Application will Reset while moving images.. \nDo you want to continue?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                    bIsMoveImagesToRetrieve = true;
                else
                    return;
            }
            else if(OperationType == "Copy")
                bIsMoveImagesToRetrieve = false;

            BackgroundWorker bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bool bIsInclude = chkInclude.IsChecked.Value ? true : false;
            object[] arrArgs = { listFilterClass, bIsInclude, bIsMoveImagesToRetrieve };

            if (OperationType == "ROI")
                bgWorker.DoWork += app.bgwDowork_AllROIExport;
            else
                bgWorker.DoWork += app.bgwDowork_RetrieveValidatedImages;
            bgWorker.ProgressChanged += app.bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(argument: arrArgs);
            app.OnWorkerMethodStart_withPercentage();
            this.Close();
        }

        private void chkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClassFormat curClass in listClassFormat)
                curClass.IsClassEnable = (sender as CheckBox).IsChecked.Value ? true : false;
        }
    }
}
