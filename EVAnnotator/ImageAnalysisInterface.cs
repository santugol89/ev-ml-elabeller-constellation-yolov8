using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int InitializeAnalysis(double nLowVal, double nHighVal, StringBuilder SaveImageType);

        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int HEProcess(StringBuilder strImageName, StringBuilder SaveImagePath);

        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int AHEProcess(StringBuilder strImageName, StringBuilder SaveImagePath);

        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int ContrastStretchProcess(StringBuilder strImageName, StringBuilder SaveImagePath);

        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LogCorrectionProcess(StringBuilder strImageName, StringBuilder SaveImagePath);

        [DllImport("GenieSupervisorLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GammaCorrectionProcess(StringBuilder strImageName, StringBuilder SaveImagePath);



        public List<ImageAnalysisMenu> ImageAnalysisList = new List<ImageAnalysisMenu>();
        public List<AnalysisModule> ListAnalysisModule = new List<AnalysisModule>();

        DateTime dtLastImgAnalysedTime = new DateTime();

        private void btnProcessAnalysis_Click(object sender, RoutedEventArgs e)
        {
            string ProcessType = "";
            string ImageType = "";
            double[] arrValues = null;
            if (ShowMessageNoProject(sender))
                return;

            if (ImageAnalysisList == null || ImageAnalysisList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load images to process Image Analysis..", "No Images", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if((sender as Telerik.Windows.Controls.RadButton).Name == "btnHEProcess")
            {
                //if (string.IsNullOrEmpty(txtHEMinVal.Text.Trim()) || string.IsNullOrEmpty(txtHEMaxVal.Text.Trim()))
                //{
                //    System.Windows.MessageBox.Show("Value field cannot be blank..", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Warning,
                //        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //    return;
                //}

                ProcessType = "HE";
                ImageType = radHEJpeg.IsChecked.Value ? ".jpeg" : ".bmp";
                arrValues = new double[] { Convert.ToDouble(txtHEMinVal.Text.Trim()), Convert.ToDouble(txtHEMaxVal.Text.Trim()) };
            }

            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnAHEProcess")
            {
                if (string.IsNullOrEmpty(txtAHEMinVal.Text.Trim()))
                {
                    System.Windows.MessageBox.Show("Value field cannot be blank..", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                ProcessType = "AHE";
                ImageType = radAHEJpeg.IsChecked.Value ? ".jpeg" : ".bmp";
                arrValues = new double[] { Convert.ToDouble(txtAHEMinVal.Text.Trim())};
            }

            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnCSProcess")
            {
                if (string.IsNullOrEmpty(txtCSMinVal.Text.Trim()) || string.IsNullOrEmpty(txtCSMaxVal.Text.Trim()))
                {
                    System.Windows.MessageBox.Show("Value field cannot be blank..", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                ProcessType = "CS";
                ImageType = radAHEJpeg.IsChecked.Value ? ".jpeg" : ".bmp";
                arrValues = new double[] { Convert.ToDouble(txtCSMinVal.Text.Trim()), Convert.ToDouble(txtCSMaxVal.Text.Trim()) };
            }

            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnLogProcess")
            {
                if (string.IsNullOrEmpty(txtLogMinVal.Text.Trim()) || string.IsNullOrEmpty(txtLogMaxVal.Text.Trim()))
                {
                    System.Windows.MessageBox.Show("Value field cannot be blank..", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                ProcessType = "LOG";
                ImageType = radAHEJpeg.IsChecked.Value ? ".jpeg" : ".bmp";
                arrValues = new double[] { Convert.ToDouble(txtLogMinVal.Text.Trim()), Convert.ToDouble(txtLogMaxVal.Text.Trim()) };
            }

            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnGammaProcess")
            {
                if (string.IsNullOrEmpty(txtGammaMinVal.Text.Trim()))
                {
                    System.Windows.MessageBox.Show("Value field cannot be blank..", "Blank..!", MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                ProcessType = "GAMMA";
                ImageType = radAHEJpeg.IsChecked.Value ? ".jpeg" : ".bmp";
                arrValues = new double[] { Convert.ToDouble(txtGammaMinVal.Text.Trim()) };
            }

            ResetAnalyisImageWindow();
            ImageAnalysisList.ForEach(item =>{
                item.AnalysisType = string.Empty;
                item.ImageProcessedPath = null;
            });
            object[] args = { ProcessType, ImageType, arrValues };
            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_ImageAnalysis;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(args);
            OnWorkerMethodStartWithPercent_ProcessFile(this, "Please wait while Processing..");
        }

        public void ImageAnalysisProcess(object[] args)
        {
            try
            {
                labelEvent.Reset();
                SaveEvent.Reset();

                if (args.Length == 0 && (args[0] as string) == string.Empty)
                    return;

                string strProcessType = args[0] as string;
                string strImageType = args[1] as string;
                double nLowVal = 0; double nHighVal = 0;
                if (args.Length > 2){
                    var tempArr = args[2] as double[];
                    nLowVal = tempArr.Length > 0 ? tempArr[0] : 0;
                    nHighVal = tempArr.Length > 1 ? tempArr[1] : 0;
                }

                int nErr = InitializeAnalysis(nLowVal, nHighVal, new StringBuilder(strImageType));
                if (nErr != 0)
                    return;

                string DateFolder = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                string strOutPutPath = settings.CSVExportPath + @"\Output Data\Image Analysis\" + DateFolder;
                if (!Directory.Exists(strOutPutPath))
                    Directory.CreateDirectory(strOutPutPath);

                bool isDiskSpaceOk = Utilities.CheckDiskSpaceOK(strOutPutPath, settings.LoadedImageSize);

                if (!isDiskSpaceOk){
                    OnWorkerMethodComplete("Complete");
                    System.Windows.MessageBox.Show("Output disk was full, Cannot process..! Free Some space and try again..", "No Storage Space", MessageBoxButton.OK,
                            MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    labelEvent.Set();
                    SaveEvent.Set();
                    if (Directory.Exists(strOutPutPath))
                        Directory.Delete(strOutPutPath);
                    return;
                }

                if (strProcessType == "HE") {
                    StartProcess(HEProcess, strProcessType, strImageType, DateFolder);
                    Dispatcher.Invoke(() => { btnHEProcess.IsEnabled = false; btnHEDisplay.IsEnabled = true; });                    
                }
                    
                else if (strProcessType == "AHE") {
                    StartProcess(AHEProcess, strProcessType, strImageType, DateFolder);
                    Dispatcher.Invoke(() => { btnAHEProcess.IsEnabled = false; btnAHEDisplay.IsEnabled = true; });                    
                }                    

                else if (strProcessType == "CS") {
                    StartProcess(ContrastStretchProcess, strProcessType, strImageType, DateFolder);
                    Dispatcher.Invoke(() => { btnCSProcess.IsEnabled = false; btnCSDisplay.IsEnabled = true;});                    
                }                    

                else if (strProcessType == "LOG") {
                    StartProcess(LogCorrectionProcess, strProcessType, strImageType, DateFolder);
                    Dispatcher.Invoke(() => { btnLogProcess.IsEnabled = false; btnLogDisplay.IsEnabled = true;});                    
                }                    

                else if (strProcessType == "GAMMA") {
                    StartProcess(GammaCorrectionProcess, strProcessType, strImageType, DateFolder);
                    Dispatcher.Invoke(() => { btnGammaProcess.IsEnabled = false; btnGammaDisplay.IsEnabled = true; });                    
                }

                string strAnalysisName = strProcessType == "HE" ? "Histogram Equalization" : strProcessType == "AHE" ? "Adaptive Histogram Equalization" :
                        strProcessType == "CS" ? "Contrast Stretching" : strProcessType == "LOG" ? "Log Correction" : strProcessType == "GAMMA" ? "Gamma Correction" : "";

                OnWorkerMethodComplete("Complete");
                MessageBox.Show("Image Analysis Completed.. Click on View Button to show Analysed Images", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Dispatcher.Invoke(() => {
                    dtLastImgAnalysedTime = DateTime.Now;
                    lblImgAnalyzeStatus.Content = "Last Image Analysed : " + strAnalysisName + " at " + dtLastImgAnalysedTime.ToShortTimeString();
                });
                labelEvent.Set();
                SaveEvent.Set();
            }

            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                labelEvent.Set();
                SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
            catch (System.Exception ex)
            {
                labelEvent.Set();
                SaveEvent.Set();
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Something went wrong..!\n" + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("MainWindow::ImageAnalysisProcess: " + ex.Message, 9);
            }
        }

        private void StartProcess(Func<StringBuilder, StringBuilder, int> FunctionProcess, string ProcessType, string strImageType, string DateFolder)
        {
            string strOutPutPath = settings.CSVExportPath + @"\Output Data\Image Analysis\" + DateFolder;

            strOutPutPath = strOutPutPath + @"\" + ProcessType + @"\";
            if (!Directory.Exists(strOutPutPath))
                Directory.CreateDirectory(strOutPutPath);

            ListAnalysisModule.RemoveAll(item => item.AnalysisName == ProcessType);
            int i = 0;
            AnalysisModule curAnalysis = new AnalysisModule(ProcessType);
            ListAnalysisModule.Add(curAnalysis);

            Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = ImageAnalysisList.Count);
            foreach (ImageAnalysisMenu curImage in ImageAnalysisList)
            {
                Dispatcher.Invoke(() => progressBar.pbStatus.Value = ++i);
                int Error = FunctionProcess(new StringBuilder(curImage.ImageAnalysisPath), new StringBuilder(strOutPutPath));

                if (Error == 0){
                    curAnalysis.ListImageProcessedPath.Add(strOutPutPath + System.IO.Path.GetFileNameWithoutExtension(curImage.ImageAnalysisName) + strImageType);
                }
            }
        }

        private void ResetAnalyisImageWindow()
        {
            ImgAnalysedImage.Source = null;
            ImgAnalysisOriginal.Source = null;
            listAnalysisImages.SelectedIndex = -1;
            lblOriginal.Visibility = Visibility.Collapsed;
            lblAnalysisType.Visibility = Visibility.Collapsed;            
        }

        public void SetAnalysisDisplayButton(bool bIstrue)
        {
            btnHEDisplay.IsEnabled = bIstrue;
            btnAHEDisplay.IsEnabled = bIstrue;
            btnCSDisplay.IsEnabled = bIstrue;
            btnGammaDisplay.IsEnabled = bIstrue;
            btnLogDisplay.IsEnabled = bIstrue;
        }

        private void SetAnalysisProcessButton(bool bSet)
        {
            btnHEProcess.IsEnabled = bSet;
            btnAHEProcess.IsEnabled = bSet;
            btnCSProcess.IsEnabled = bSet;
            btnLogProcess.IsEnabled = bSet;
            btnGammaProcess.IsEnabled = bSet;
        }

        private void btnDisplayImage_Click(object sender, RoutedEventArgs e)
        {
            if (ImageAnalysisList == null || ImageAnalysisList.Count == 0)
                return;

            string strProcessType = "";
            if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnHEDisplay")
                strProcessType = "HE";
            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnAHEDisplay")
                strProcessType = "AHE";
            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnCSDisplay")
                strProcessType = "CS";
            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnLogDisplay")
                strProcessType = "LOG";
            else if ((sender as Telerik.Windows.Controls.RadButton).Name == "btnGammaDisplay")
                strProcessType = "GAMMA";

            ResetAnalyisImageWindow();
            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_DisplayImageAnalysis;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync(strProcessType);
            OnWorkerMethodStartWithPercent_ProcessFile(this, "Please wait while Reading Images..");
        }

        private void ImageAnalysisDisplay(string strProcessType)
        {
            try
            {
                string strAnalysisName = strProcessType == "HE" ? "Histogram Equalization" : strProcessType == "AHE" ? "Adaptive Histogram Equalization" :
                                    strProcessType == "CS" ? "Contrast Stretching" : strProcessType == "LOG" ? "Log Correction" : strProcessType == "GAMMA" ? "Gamma Correction" : "";

                var tempList = ListAnalysisModule.FirstOrDefault(item => item.AnalysisName == strProcessType);

                if (tempList == null)
                    return;
                int i = 0;
                Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = ImageAnalysisList.Count);
                foreach (ImageAnalysisMenu curAnalysis in ImageAnalysisList)
                {
                    Dispatcher.Invoke(() => progressBar.pbStatus.Value = ++i);
                    curAnalysis.AnalysisType = strAnalysisName;
                    curAnalysis.ImageProcessedPath = tempList.ListImageProcessedPath.FirstOrDefault(item => item.Contains(Path.GetFileNameWithoutExtension(curAnalysis.ImageAnalysisName)));
                }

                OnWorkerMethodComplete("Complete");
                Dispatcher.Invoke(() =>
                {
                    //listAnalysisImages.SelectionChanged += listAnalysisImages_SelectionChanged;
                    if (listAnalysisImages.Items.Count > 0)
                        listAnalysisImages.SelectedIndex = 0;
                    //listAnalysisImages.SelectionChanged -= listAnalysisImages_SelectionChanged;
                });
            }

            catch (System.Exception ex)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Something went wrong..!\n" + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void  listAnalysisImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (listAnalysisImages.SelectedItem == null)
                    return;

                ImgAnalysedImage.Source = null;
                ImgAnalysisOriginal.Source = null;
                lblOriginal.Visibility = Visibility.Collapsed;
                lblAnalysisType.Visibility = Visibility.Collapsed;

                ImageAnalysisMenu currentImage = listAnalysisImages.SelectedItem as ImageAnalysisMenu;
                BitmapImage bmpImage = new BitmapImage();
                try
                {
                    using (FileStream stream = Delimon.Win32.IO.File.OpenRead(currentImage.ImageAnalysisPath))
                    {
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = stream;
                        bmpImage.EndInit();
                        ImgAnalysisOriginal.Source = bmpImage;
                    }

                    if(currentImage.ImageProcessedPath != null){
                        bmpImage = new BitmapImage();
                        using (FileStream stream = Delimon.Win32.IO.File.OpenRead(currentImage.ImageProcessedPath))
                        {
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = stream;
                            bmpImage.EndInit();
                            ImgAnalysedImage.Source = bmpImage;
                        }
                        lblAnalysisType.Visibility = Visibility.Visible;
                        lblAnalysisType.Content = currentImage.AnalysisType;
                    }
                    lblOriginal.Visibility = Visibility.Visible;
                    lblOriginal.Content = "Original Image";
                }

                catch
                {
                    ImgAnalysisOriginal.Source = null;
                    ImgAnalysedImage.Source = null;
                    lblOriginal.Visibility = Visibility.Visible;
                    lblAnalysisType.Visibility = Visibility.Collapsed;
                    lblOriginal.Content = "Corrupt Image File.. Loading Failed";
                }
            }

            catch (Exception ex)
            {
                Utilities.LogMessage("listAnalysisImages_SelectionChangedEvent: " + ex.ToString(), 0);
            }
        }

        private void txtConfigVal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            if ((sender as TextBox).Text.Length > 4)
                e.Handled = true;

            if (regex.IsMatch(e.Text))
                e.Handled = true;
        }

        double nHEMinVal = 0;
        double nHEMaxVal = 0;

        private string _nAHEMinVal = "0.5";
        public string nAHEMinVal
        {
            get { return _nAHEMinVal; }
            set
            {
                _nAHEMinVal = value;
                btnAHEProcess.IsEnabled = true;
                btnAHEDisplay.IsEnabled = false;
            }
        }

        private string _nCSContrastVal = "1";
        public string nCSContrastVal
        {
            get { return _nCSContrastVal; }
            set
            {
                _nCSContrastVal = value;
                btnCSProcess.IsEnabled = true;
                btnCSDisplay.IsEnabled = false;
            }
        }

        private string _nCSBrightnessVal = "1";
        public string nCSBrightnessVal
        {
            get { return _nCSBrightnessVal; }
            set
            {
                _nCSBrightnessVal = value;
                btnCSProcess.IsEnabled = true;
                btnCSDisplay.IsEnabled = false;
            }
        }

        private string _nLogAlphaVal = "1";
        public string nLogAlphaVal
        {
            get { return _nLogAlphaVal; }
            set
            {
                _nLogAlphaVal = value;
                btnLogProcess.IsEnabled = true;
                btnLogDisplay.IsEnabled = false;
            }
        }

        private string _nLogBetaVal = "255";
        public string nLogBetaVal
        {
            get { return _nLogBetaVal; }
            set
            {
                _nLogBetaVal = value;
                btnLogProcess.IsEnabled = true;
                btnLogDisplay.IsEnabled = false;
            }
        }

        private string _nGammaRatio = "0.5";
        public string nGammaRatio
        {
            get { return _nGammaRatio; }
            set
            {
                _nGammaRatio = value;
                btnGammaProcess.IsEnabled = true;
                btnGammaDisplay.IsEnabled = false;
            }
        }

        private void radAnalysisButton_Checked(object sender, RoutedEventArgs e)
        {
            if (btnHEProcess == null || btnAHEProcess == null || btnCSProcess == null || btnLogProcess == null || btnGammaProcess == null)
                return;

            if(((sender as Telerik.Windows.Controls.RadRadioButton).Name == "radHEBmp" || (sender as Telerik.Windows.Controls.RadRadioButton).Name == "radHEJpeg"))
            {
                btnHEProcess.IsEnabled = true;
                btnHEDisplay.IsEnabled = false;
            }
            else if (((sender as Telerik.Windows.Controls.RadRadioButton).Name == "radAHEBmp" || (sender as Telerik.Windows.Controls.RadRadioButton).Name == "radAHEJpeg"))
            {
                btnAHEProcess.IsEnabled = true;
                btnAHEDisplay.IsEnabled = false;
            }
            else if (((sender as Telerik.Windows.Controls.RadRadioButton).Name == "radCSBmp" || (sender as Telerik.Windows.Controls.RadRadioButton).Name == "radCSJpeg"))
            {
                btnCSProcess.IsEnabled = true;
                btnCSDisplay.IsEnabled = false;
            }
            else if (((sender as Telerik.Windows.Controls.RadRadioButton).Name == "radLogBmp" || (sender as Telerik.Windows.Controls.RadRadioButton).Name == "radLogJpeg"))
            {
                btnLogProcess.IsEnabled = true;
                btnLogDisplay.IsEnabled = false;
            }
            else if (((sender as Telerik.Windows.Controls.RadRadioButton).Name == "radGammaBmp" || (sender as Telerik.Windows.Controls.RadRadioButton).Name == "radGammaJpeg"))
            {
                btnGammaProcess.IsEnabled = true;
                btnGammaDisplay.IsEnabled = false;
            }
        }
    }

    public class ImageAnalysisMenu
    {
        public string ImageAnalysisPath { get; set; }

        public string ImageAnalysisName { get; set; }

        public string ImageProcessedPath { get; set; }

        public string AnalysisType { get; set; }

        public ImageAnalysisMenu(string strImagePath)
        {
            ImageAnalysisPath = strImagePath;
            ImageAnalysisName = System.IO.Path.GetFileName(strImagePath);
        }
    }

    public class AnalysisModule
    {
        public string AnalysisName { get; set; }

        public List<string> ListImageProcessedPath { get; set; }

        public AnalysisModule(string strName)
        {
            AnalysisName = strName;
            ListImageProcessedPath = new List<string>();
        }
    }
}
