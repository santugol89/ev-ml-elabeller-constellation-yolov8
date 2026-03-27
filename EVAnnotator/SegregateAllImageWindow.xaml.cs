using GenieSupervisor.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for AddMultipleImportFile.xaml
    /// </summary>
    public partial class SegregateAllImageWindow : RadWindow, INotifyPropertyChanged
    {
        MainWindow app;
        public event PropertyChangedEventHandler PropertyChanged;
        BackgroundWorker BGWorker;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SegregateAllImageWindow(MainWindow app)
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
                ButtonClose_Click(null,null);
        }

        private void ButtonClose_Click(object value1, object value2)
        {
            this.Close();
        }

        private void InitializeControls()
        {
            string[] arrayValue = app.settings.dictEVSupervisorClass.Values.ToArray();
            for (int i = 0; i < arrayValue.Length; i++)
                cmbClassNames.Items.Add(arrayValue[i].ToString());
        }

        private void btnSegregateAll_Click(object sender, RoutedEventArgs e)
        {
            if(cmbClassNames.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select ClassName to Segregate Images.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            BGWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            BGWorker.DoWork += bgwDowork_SegregateAllImages;
            BGWorker.ProgressChanged += app.bgwProgressChange_Load;

            object[] args = { chkIncludeLabelled.IsChecked.Value, cmbClassNames.SelectedItem.ToString() };
            BGWorker.RunWorkerAsync(args);
            app.OnWorkerMethodStart_withPercentage();
        }

        private async void bgwDowork_SegregateAllImages(object sender, DoWorkEventArgs e)
        {
            if (BGWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                object[] args = e.Argument as object[];
                await Task.Run(() =>
                {
                    app.SegregateAllLoadedImages(args);
                });

                Dispatcher.Invoke(() => this.Close());
            }
        }
    }
}
