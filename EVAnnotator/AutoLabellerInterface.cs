using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //--------ML Prediction for BV and IPIeML---------------------
        [DllImport("GenieLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfInitialize(StringBuilder strModelPath, int nClassCount, double Threshold, double Nms, double Scale, int ImageSize,
                        StringBuilder InputLayer, StringBuilder OutputLayer1, StringBuilder OutputLayer2, StringBuilder OutputLayer3, string[] classes, bool isGPU);

        [DllImport("GenieLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfPredict(StringBuilder strImagePath, ref MarshalStruct pointerinStruct); 

        [DllImport("GenieLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfPredictIPIe(StringBuilder strImagePath, ref MarshalStruct pointerinStruct); 

        [DllImport("GenieLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void FreeMarshalStructPtr(ref MarshalStruct pointerinStruct);
        //-------------------------------------------------------------------------------------------

        //----------ML Prediction for EBML or IPIeML--------------------
        [DllImport("GenieLibAladdin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfInitializeDetection(StringBuilder strModelPath, int nClassCount, int nPassClassCount, int nFailClassCoiunt, double Score, double NMS, int ImageWidth, int ImageHeight,
                StringBuilder InputLayer, StringBuilder OutputLayer1, StringBuilder OutputLayer2, StringBuilder OutputLayer3, string[] classes, bool isGPU, double[] VariableThreshold,
                string[] PassClasses, string[] FailClasses, int PixelWidth, string[] chartColors, string strDebugTimeLogPath, int nAnchorTotal, string[] DetectionAnchors);

        [DllImport("GenieLibAladdin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfPredictEBML(StringBuilder strImagePath, StringBuilder strSavePath, bool bSaveImage, bool ShowScore, int TrackNo, bool bIsEnableDebugLog, ref MarshalStruct pointerinStruct, ref double PredictTime);


        [DllImport("GenieLibAladdin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfPredictIPIe(StringBuilder strImagePath, StringBuilder strSavePath, bool bSaveImage, bool ShowScore, int TrackNo, bool bIsEnableDebugLog, ref MarshalStruct pointerinStruct, ref double PredictTime);


        [DllImport("GenieLibLPC.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfInitializeLPCDetection(StringBuilder strModelPath, int nClassCount, int nPassClassCount, int nFailClassCoiunt, double Score, double NMS, int ImageWidth, int ImageHeight,
                StringBuilder InputLayer, StringBuilder OutputLayer1, StringBuilder OutputLayer2, StringBuilder OutputLayer3, string[] classes, bool isGPU, double[] VariableThreshold,
                string[] PassClasses, string[] FailClasses, int PixelWidth, string[] chartColors, string strDebugTimeLogPath, int nAnchorTotal, string[] DetectionAnchors);


        [DllImport("GenieLibLPC.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int TfPredictYoloV3(StringBuilder strImagePath, StringBuilder strSavePath, bool bSaveImage, bool ShowScore, int TrackNo, bool bIsEnableDebugLog, ref MarshalStruct pointerinStruct, ref double PredictTime, bool bIsLogPredictTime);


        [DllImport("GenieLibAladdin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void FreeMarshalStructPtrAladdin(ref MarshalStruct pointerinStruct);
        //-----------------------------------------------------------------

        DateTime dtLastAutoPilotTime = new DateTime();
        public SolidColorBrush[] StatckedGraphBrushes = new SolidColorBrush[] { Brushes.DarkOrange, Brushes.MediumVioletRed };

        ImageStatsWindow imageStatsWindow = null;
        private void btnImage_stats_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load images...", "No Images", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (imageStatsWindow == null)
            {
                imageStatsWindow = new ImageStatsWindow(this);
                imageStatsWindow.Show();
                return;
            }

            else if (!imageStatsWindow.IsActive)
            {
                imageStatsWindow.Close();
                imageStatsWindow = new ImageStatsWindow(this);
                imageStatsWindow.Show();
                return;
            }
        }

        private void btnAutoPilot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ShowMessageNoProject(sender))
                return;

            if (ImageMenuList == null || ImageMenuList.Count == 0)
            {
                System.Windows.MessageBox.Show("Please load images to start Auto Pilot..", "No Images", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if(string.IsNullOrEmpty(settings.ModelPath) || !Directory.Exists(settings.ModelPath))
            {
                System.Windows.MessageBox.Show("Please select proper model path to continue from File->Settings..", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.None, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Please confirm to start Auto Pilot mode?", "Auto Pilot", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result == MessageBoxResult.No)
            {
                return;
            }

            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgwDowork_AutoLabellerProcess;
            bgWorker.ProgressChanged += bgwProgressChange_Load;
            bgWorker.RunWorkerAsync();
            OnWorkerMethodStartWithPercent_ProcessFile(this, "Auto Pilot mode is running.. Please wait..");
        }

        List<string> IpieVisionBucketList = new List<string>();
        string modelType = null;
        private bool LoadModel()
        {
             modelType = settings.dictProjectList[settings.CurrentProject].Contains("BV") ? "BV" : settings.dictProjectList[settings.CurrentProject].Contains("IPIe") ? "IPIe" :
                                settings.dictProjectList[settings.CurrentProject].Contains("LPC") ? "LPC" : settings.dictProjectList[settings.CurrentProject].Contains("Enhanced Blister")? "EB" : "";
            string[] arrModelFiles = Directory.GetFiles(settings.ModelPath, "*" + modelType + "*.pb", SearchOption.AllDirectories);
            if (arrModelFiles.Length < 1)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("No \"" + modelType + "\" model files found.. \nPlease select Proper model path in File->Settings", "Load Model Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string[] arriniFiles = Directory.GetFiles(settings.ModelPath, "*" + modelType + "*.ini", SearchOption.AllDirectories);
            if (arriniFiles.Length < 1)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("No \"" + modelType + "\" ini files found.. \nPlease select Proper model path in File->Settings", "Load Model Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            bool bIsLoaded;
            if (modelType == "EB" || modelType == "IPIe" || modelType == "LPC")
                bIsLoaded = LoadModelParameterandInitializeForAladdin(arriniFiles, arrModelFiles);
            else
                bIsLoaded =  LoadModelParameterandInitialize(arriniFiles, arrModelFiles);

            return bIsLoaded;
        }

        public List<Brush> modelClassBrushes = new List<Brush> { Brushes.ForestGreen,
                Brushes.DarkOrange, Brushes.MediumVioletRed, Brushes.Lavender, Brushes.MediumOrchid, Brushes.Firebrick,
                Brushes.Yellow, Brushes.RoyalBlue, Brushes.BurlyWood, Brushes.Tomato,
                Brushes.Cyan, Brushes.Plum, Brushes.MediumSlateBlue, Brushes.Plum, Brushes.BlanchedAlmond, Brushes.LightSlateGray,
                Brushes.Lavender, Brushes.Tomato, Brushes.Cyan, Brushes.LightSkyBlue, Brushes.WhiteSmoke, Brushes.CadetBlue,
                Brushes.BlanchedAlmond, Brushes.Firebrick};

        public string ModelName;
        public double DetectionScale;
        public int DetectionImgWidth, DetectionImgHeight;
        public string[] DetectionInputlayer;
        public string[] DetectionOutputlayer;
        public int DetectionClassCount;
        public List<string> DetectionFailClasses = new List<string>();
        public List<string> DetectionPassClasses = new List<string>();
        public bool bisGPU = true;
        public double DetectionScore;
        public double DetectionNMS;
        public double[] DetectionClasswiseThreshold;
        public List<string> DetectionVisionBucket = new List<string>();
        public bool bShowScore = false;
        public int DetectionPixelWidth;
        public List<string> DetectionAnchorList = new List<string>();
        public int nAnchorTotal = 0;

        public Dictionary<int, string> dictModelClassList = new Dictionary<int, string>();
        public Dictionary<string, string> dictModelClassFullName = new Dictionary<string, string>();

        private void LoadModelParameter(string[] arriniFiles)
        {
            IniFile iniLabel = new IniFile(arriniFiles[0]);
            string str;
            ModelName = iniLabel.ReadValue("DetectionModelInfo", "Station", "");
            bisGPU = iniLabel.ReadValue("DetectionModelInfo", "GPU", true);
            bShowScore = iniLabel.ReadValue("DetectionModelInfo", "ShowScore", false);
            DetectionClassCount = iniLabel.ReadValue("DetectionModelInfo", "Classes", 0);
            DetectionPixelWidth = iniLabel.ReadValue("DetectionModelInfo", "PixelWidth", 5);
            DetectionScale = iniLabel.ReadValue("DetectionParameter", "Scale", 0f);
            DetectionImgWidth = iniLabel.ReadValue("DetectionParameter", "ImageWidth", 0);
            DetectionImgHeight = iniLabel.ReadValue("DetectionParameter", "ImageHeight", 0);
            DetectionScore = Math.Round(iniLabel.ReadValue("DetectionThresholds", "Score", 0f), 2);
            DetectionNMS = iniLabel.ReadValue("DetectionThresholds", "NMS", 0f);
            DetectionInputlayer = new string[1];
            DetectionOutputlayer = new string[3];
            nAnchorTotal = iniLabel.ReadValue("DetectionModelInfo", "Anchors", 0);

            for (int i = 0; i < DetectionInputlayer.Length; i++)
                DetectionInputlayer[i] = iniLabel.ReadValue("DetectionParameter", "InputLayer", "");

            for (int i = 0; i < DetectionOutputlayer.Length; i++)
                DetectionOutputlayer[i] = iniLabel.ReadValue("DetectionParameter", string.Format("OutputLayer{0}", i), "");

            dictModelClassList = new Dictionary<int, string>();
            dictModelClassFullName = new Dictionary<string, string>();
            int index = 0;
            while (true)
            {
                if (dictModelClassList.Count == DetectionClassCount)
                    break;

                str = iniLabel.ReadValue("DetectionClass", String.Format("C{0}", index), "");
                if (String.IsNullOrEmpty(str))
                {
                    if (index < 1)
                    {
                        index++;
                        continue;
                    }
                    else
                        break;
                }

                str.Trim();

                if (!dictModelClassFullName.ContainsValue(str))
                {
                    dictModelClassList.Add(index, str);

                    string strClass = iniLabel.ReadValue("DetectionClassFullName", str, "");
                    if (String.IsNullOrEmpty(strClass))
                    {
                        index++;
                        continue;
                    }
                    dictModelClassFullName.Add(str, strClass);
                }
                index++;
            }

            DetectionFailClasses = new List<string>();
            DetectionPassClasses = new List<string>();
            DetectionVisionBucket = new List<string>();
            DetectionClasswiseThreshold = new double[DetectionClassCount];
            for (int count = 0; count < dictModelClassList.Count + 1; count++)
            {
                str = iniLabel.ReadValue("DetectionFailClasses", String.Format("C{0}", count), "");
                str.Trim();

                if (!string.IsNullOrEmpty(str) && !DetectionFailClasses.Contains(str))
                    DetectionFailClasses.Add(str);

                str = iniLabel.ReadValue("DetectionPassClasses", String.Format("C{0}", count), "");
                str.Trim();

                if (!string.IsNullOrEmpty(str) && !DetectionPassClasses.Contains(str))
                    DetectionPassClasses.Add(str);

                str = iniLabel.ReadValue("VisionBucket", String.Format("{0}", count), "");
                str.Trim();

                if (!string.IsNullOrEmpty(str) && !DetectionVisionBucket.Contains(str))
                    DetectionVisionBucket.Add(str);

                if (count < DetectionClassCount)
                    DetectionClasswiseThreshold[count] = Math.Round(iniLabel.ReadValue("DetectionClassWiseThreshold", dictModelClassList.ElementAt(count).Value, 0f), 4);
            }

            for (int ind = 0; ind < nAnchorTotal; ind++)
            {
                string strAnchor = iniLabel.ReadValue("DetectionAnchors", String.Format("{0}", ind), "");
                DetectionAnchorList.Add(strAnchor);
            }
        }

        private bool LoadModelParameterandInitializeForAladdin(string[] arriniFiles, string[] arrModelFiles)
        {
            LoadModelParameter(arriniFiles);

            if (dictModelClassList.Count == 0)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Unable to read class information from file " + System.IO.Path.GetFileName(arriniFiles[0]), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string modelPbFile = arrModelFiles.FirstOrDefault(temp => temp.Contains(modelType + "D"));
            if (string.IsNullOrEmpty(modelPbFile))
                modelPbFile = arrModelFiles[0];

            string DebugLogSavePath = settings.StatsFilePath + "\\TimeLogs.txt";
            File.AppendAllText(DebugLogSavePath, "--------------------------------------------\n");
            File.AppendAllText(DebugLogSavePath, "Date : " + DateTime.Now.ToString() + "\n");

            bool bisBGExist = dictModelClassList.Values.Contains("UP");
            string[] chartColors = bisBGExist ? modelClassBrushes.Select(temp => temp.ToString()).Take(dictModelClassList.Count - 1).ToArray() :
                                     modelClassBrushes.Select(temp => temp.ToString()).Take(dictModelClassList.Count).ToArray();
            var arrClasses = bisBGExist ? dictModelClassList.Values.Take(dictModelClassList.Count - 1) : dictModelClassList.Values;    //Last background class is excluded
            var arrFailClasses = bisBGExist ? DetectionFailClasses.Take(DetectionFailClasses.Count - 1) : DetectionFailClasses;   //Last background class is excluded

            int error = -1;
            if(modelType == "LPC")
            {
                error = TfInitializeLPCDetection(new StringBuilder(modelPbFile), arrClasses.Count(), DetectionPassClasses.Count, arrFailClasses.Count(), DetectionScore, DetectionNMS, DetectionImgWidth, DetectionImgHeight,
                    new StringBuilder(DetectionInputlayer[0]), new StringBuilder(DetectionOutputlayer[0]), new StringBuilder(DetectionOutputlayer[1]), new StringBuilder(DetectionOutputlayer[2]),
                    arrClasses.ToArray(), bisGPU, DetectionClasswiseThreshold, DetectionPassClasses.ToArray(), arrFailClasses.ToArray(), DetectionPixelWidth, chartColors, DebugLogSavePath, nAnchorTotal, DetectionAnchorList.ToArray());
            }
            else
            {
                error = TfInitializeDetection(new StringBuilder(modelPbFile), arrClasses.Count(), DetectionPassClasses.Count, arrFailClasses.Count(), DetectionScore, DetectionNMS, DetectionImgWidth, DetectionImgHeight,
                        new StringBuilder(DetectionInputlayer[0]), new StringBuilder(DetectionOutputlayer[0]), new StringBuilder(DetectionOutputlayer[1]), new StringBuilder(DetectionOutputlayer[2]),
                        arrClasses.ToArray(), bisGPU, DetectionClasswiseThreshold, DetectionPassClasses.ToArray(), arrFailClasses.ToArray(), DetectionPixelWidth, chartColors, DebugLogSavePath, nAnchorTotal, DetectionAnchorList.ToArray());
            }
            string message;
            if (error == -1)
                message = "Model file not found";
            else if (error == -2)
                message = "Error in reading model file";
            else if (error == -3)
                message = "Error in loading model";
            else if (error == -4)
                message = "Bypassed loading model: No class information found in ini file";
            else
                message = String.Format("Tensorflow Initialization done;  Model: {0};  ClassCount: {1};  DetectionScore: {2};  DetectionNMS: {3};  DetectionimageWidth: {4};  DetectionimageHeight: {5};  Layers: input=\"{6}\" output=\"{7}\" output=\"{8}\" output=\"{9}\"",
                    modelPbFile, dictModelClassList.Count, DetectionScore, DetectionNMS, DetectionImgWidth, DetectionImgHeight, DetectionInputlayer[0], DetectionOutputlayer[0], DetectionOutputlayer[1], DetectionOutputlayer[2]);

            Utilities.LogMessage(message);

            if (error != 0)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool LoadModelParameterandInitialize(string[] iniFile, string[] arrModelFiles)
        {
            IniFile iniLabel = new IniFile(iniFile[0]);
            string str;
            string modelName = iniLabel.ReadValue("Model Info", "Station", "");
            bool bIsGPU = iniLabel.ReadValue("Model Info", "GPU", true);
            int DetectionimageHeight = iniLabel.ReadValue("Detection Parameters", "InputDimensions", 576);
            int DetectionimageWidth = DetectionimageHeight;
            double Detectionscale = iniLabel.ReadValue("Detection Parameters", "Scale", 0d);
            double DetectionScore = iniLabel.ReadValue("Detection Thresholds", "Score", 0d);
            double DetectionNMS = iniLabel.ReadValue("Detection Thresholds", "NMS", 0d);
            string[] DetectioninputLayer = new string[iniLabel.ReadValue("Detection Model Info", "InputLayerCount", 1)];
            string[] DetectionoutputLayer = new string[iniLabel.ReadValue("Detection Model Info", "OutputLayerCount", 3)];

            for (int i = 0; i < DetectioninputLayer.Length; i++)
                DetectioninputLayer[i] = iniLabel.ReadValue("Detection Parameters", string.Format("InputLayer{0}", (i + 1)), "");

            for (int i = 0; i < DetectionoutputLayer.Length; i++)
                DetectionoutputLayer[i] = iniLabel.ReadValue("Detection Parameters", string.Format("OutputLayer{0}", (i + 1)), "");

            //if (modelType == "IPIe")
            //{
            //    IpieVisionBucketList = new List<string>();
            //    int idx = 1;
            //    while (true)
            //    {
            //        str = iniLabel.ReadValue("VisionBucket", String.Format("{0}", idx), "");
            //        if (String.IsNullOrEmpty(str))
            //        {
            //            if (idx < 1)
            //            {
            //                idx++;
            //                continue;
            //            }
            //            else
            //                break;
            //        }
            //        if (String.IsNullOrEmpty(str))
            //            break;
            //        str.Trim();

            //        if (!IpieVisionBucketList.Contains(str))
            //            IpieVisionBucketList.Add(str);

            //        idx++;
            //    }
            //}
            dictModelClassList = new Dictionary<int, string>();
            int index = 0;
            while (true)
            {
                str = iniLabel.ReadValue("Class Alias", String.Format("C{0}", index), "");
                if (String.IsNullOrEmpty(str))
                {
                    if (index < 1)
                    {
                        index++;
                        continue;
                    }
                    else
                        break;
                }
                str.Trim();

                if (!dictModelClassList.ContainsValue(str))
                    dictModelClassList.Add(index, str);

                index++;
            }

            if (dictModelClassList.Count == 0)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("Unable to read class information from file " + System.IO.Path.GetFileName(iniFile[0]), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string modelPbFile = arrModelFiles.FirstOrDefault(temp => temp.Contains(modelType + "D"));
            if (string.IsNullOrEmpty(modelPbFile))
                modelPbFile = arrModelFiles[0];
            int error = TfInitialize(new StringBuilder(modelPbFile), dictModelClassList.Count, DetectionScore, DetectionNMS, Detectionscale,
                            DetectionimageHeight, new StringBuilder(DetectioninputLayer[0]), new StringBuilder(DetectionoutputLayer[0]), new StringBuilder(DetectionoutputLayer[1]), new StringBuilder(DetectionoutputLayer[2]), dictModelClassList.Values.ToArray(), bIsGPU);

            string message;
            if (error == -1)
                message = "Model file not found";
            else if (error == -2)
                message = "Error in reading model file";
            else if (error == -3)
                message = "Error in loading model";
            else if (error == -4)
                message = "Bypassed loading model: No class information found in ini file";
            else
                message = String.Format("Tensorflow Initialization done;  Model: {0};  ClassCount: {1};  DetectionScore: {2};  DetectionNMS: {3};  DetectionimageWidth: {4};  DetectionimageHeight: {5};  Layers: input=\"{6}\" output=\"{7}\" output=\"{8}\" output=\"{9}\"",
                    modelPbFile, dictModelClassList.Count, DetectionScore, DetectionNMS, DetectionimageHeight, DetectionimageHeight, DetectioninputLayer[0], DetectionoutputLayer[0], DetectionoutputLayer[1], DetectionoutputLayer[2]);

            Utilities.LogMessage(message);

            if (error != 0)
            {
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MarshalStruct
        {
            public int outArray_length;
            public int inArray_length;
            public IntPtr array;
        };
        
        public void AutoLabellerProcess()
        {
            try
            {
                if (!bIsModelLoad)
                {
                    Utilities.LogMessage("Load model Initiated");
                    bool success = LoadModel();
                    if (success)
                        bIsModelLoad = true;
                    else
                        return;
                }

                labelEvent.Reset();
                SaveEvent.Reset();
                settings.ImportFilePath = null;
                CleanupLoadedData();

                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                List<string[]> tempDictList = new List<string[]>();
                foreach (var curItem in settings.dictEVSupervisorClass)
                    tempDictList.Add(new string[3] { curItem.Key.ToString(), curItem.Value, curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0] });

                //string strDataPath = settings.CSVExportPath + @"\Output Data\Augmentation\" + DateTime.Now.ToString("ddMMyyyy_HHmmss");
                //if (!Directory.Exists(strDataPath))
                //    Directory.CreateDirectory(strDataPath);
                //string strCSVSavePath = System.IO.Path.Combine(strDataPath, "augment_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv");
                var rejectedImages = new List<string>();
                int nSkippedImages = 0;
                List<string> listDistinctImagePaths = settings.LoadedImagefiles.DistinctBy(item => System.IO.Path.GetFileName(item)).ToList();
                Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = listDistinctImagePaths.Count);
                for (int count = 0; count < listDistinctImagePaths.Count; count++)
                {
                    Dispatcher.Invoke(() => progressBar.pbStatus.Value = count);
                    string strImagePath = listDistinctImagePaths[count];

                    //Rejecting Non-VisionBucket images in IPIe project only
                    //Puneeth H.S Code
                    bool skipImage = false;
                    //if (modelType == "IPIe" && IpieVisionBucketList.Count > 0)
                    //{
                    //    string imageName = System.IO.Path.GetFileName(strImagePath);
                    //    if(skipImage = !(IpieVisionBucketList.Any(s => imageName.Contains(s))))
                    //    {
                    //        rejectedImages.Add(imageName);
                    //        nSkippedImages++;
                    //    }
                    //}

                    if (skipImage)
                        continue;
                    //Prediction from dll 
                    int nError = -1;
                    double[][] arrAttributes = PredictImageClass(strImagePath, out nError);

                    //to prevent memory leakage in c++ dll
                    if (nError == -5)
                    {
                        labelEvent.Set();
                        SaveEvent.Set();
                        Dispatcher.Invoke(() => {
                            UpdateAutoLabellerClass2DGraph(settings.CurrentProject);
                            lvAutoPilotBVStat.ItemsSource = null;
                            lvAutoPilotIPIeStat.ItemsSource = null;
                        });
                        AutoPilotTotalImages = 0;
                        AutoPilotNonProcImages = nSkippedImages;
                        OnWorkerMethodComplete("Complete");
                        System.Windows.MessageBox.Show("Cannot continued with selected images.. \nPlease load proper images..", "Error in process..!", MessageBoxButton.OK,
                            MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }

                    if (arrAttributes == null)
                    {
                        nSkippedImages++;
                        continue;
                    }

                    char strImageType = System.IO.Path.GetFileName(strImagePath).Contains(settings.SinglePhase) ? 'S' : System.IO.Path.GetFileName(strImagePath).Contains(settings.PhaseContrast) ? 'P' : ' ';
                    for (int i = 0; i < arrAttributes.Length; i++)
                    {
                        ImageListBox curImageBox = ProcessedImageBox.FirstOrDefault(item => item.ImageBoxName == System.IO.Path.GetFileName(strImagePath));
                        string classindex = arrAttributes[i][5].ToString();
                        string classAlias = dictModelClassList.ElementAt(Convert.ToInt16(classindex)).Value;
                        string classid = dictModelClassList.ElementAt(Convert.ToInt16(classindex)).Key.ToString() ;
                        string className = "";

                        var tempClassItem = tempDictList.FirstOrDefault(temp => temp[2].ToUpper() == classAlias.ToUpper());
                        if (tempClassItem != null)
                            className = tempClassItem[1];
                        else
                            className = "Unknown Class" + "(" + classAlias + ")";

                        ClassFolderStat curclassFolder = ListClassFolderStat.FirstOrDefault(item => item.ClassAliasName.ToUpper() == classAlias.ToUpper());
                        if (curclassFolder == null)
                        {
                            ListClassFolderStat.Add(new ClassFolderStat
                            {
                                ImportDatasheetName = "",
                                ClassCount = 1,
                                ClassAliasName = classAlias,
                                ClassFolderName = className,
                                ClassID = classid,
                                SingleSpotCount = strImageType == 'S' ? 1 : 0,
                                PhaseContrastCount = strImageType == 'P' ? 1 : 0
                            });
                        }
                        else
                        {
                            curclassFolder.ClassCount++;
                            if (strImageType == 'S')
                                curclassFolder.SingleSpotCount++;
                            else if (strImageType == 'P')
                                curclassFolder.PhaseContrastCount++;
                        }

                        ImageClass curImageClass = new ImageClass(classid, className);
                        EnumSelectedShape curShape = arrAttributes[i].Length > 6 ? EnumSelectedShape.Polyline : EnumSelectedShape.Rectangle;
                        curImageClass.ClassAlias = classAlias;

                        if (curShape == EnumSelectedShape.Rectangle)
                        {
                            curImageClass.XCoordinate = Math.Round(arrAttributes[i][0], 3);
                            curImageClass.YCoordinate = Math.Round(arrAttributes[i][1], 3);
                            curImageClass.Width = Math.Round(arrAttributes[i][2] - arrAttributes[i][0], 3);
                            curImageClass.Height = Math.Round(arrAttributes[i][3] - arrAttributes[i][1], 3);
                            curImageClass.Shape = curShape;
                            curImageClass.Score = (Math.Round(arrAttributes[i][4], 2) * 100).ToString() + "%";
                            string shape = "rect";
                            curImageClass.ShapeCoordinates = "{\"name\":\"" + shape + "\", \"x\": " + curImageClass.XCoordinate + ", \"y\": " + curImageClass.YCoordinate +
                                                    ", \"width\": " + curImageClass.Width + ", \"height\": " + curImageClass.Height + " }";
                        }

                        curImageClass.Reviewed = false;
                        if (curImageBox == null)
                        {
                            curImageBox = new ImageListBox(System.IO.Path.GetFileName(strImagePath));
                            ProcessedImageBox.Add(curImageBox);
                        }
                        curImageBox.ListImageClass.Add(curImageClass);
                    }
                }

                ImageClassMatching();
                dtLastAutoPilotTime = DateTime.Now;
                AutoPilotTotalImages = listDistinctImagePaths.Count;
                AutoPilotNonProcImages = nSkippedImages;
                Dispatcher.Invoke(() => {
                    UpdateAutoLabellerClass2DGraph(settings.CurrentProject);
                    SaveAutoLabelledStatHistory();
                });

                //Puneeth H.S Code
                //if (modelType == "IPIe" && rejectedImages.Count > 0)
                //{
                //    string strDataPath = settings.CSVExportPath + @"\Output Data\";
                //    if (!Directory.Exists(strDataPath))
                //        Directory.CreateDirectory(strDataPath);
                //    var rejectedImagesTxt = string.Join("\n", rejectedImages);
                //    File.AppendAllText(strDataPath + @"\Rejected images_" + DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss") + ".txt", rejectedImagesTxt);
                //}

                OnWorkerMethodComplete("Complete");
                if (AutoPilotTotalImages == AutoPilotNonProcImages)
                    System.Windows.MessageBox.Show("No images are processed..", "Information", MessageBoxButton.OK,
                                    MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);

                Utilities.LogMessage("AutoLabellerProcess successfully done", 0);

                labelEvent.Set();
                SaveEvent.Set();
                bIsLoadLabellingGraph = true;
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
                Utilities.LogMessage("MainWindow::AutoLabellerProcess: " + ex.Message, 9);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private double[][] PredictImageClass(string imagePath, out int nError)
        {
            MarshalStruct pointerinStruct = new MarshalStruct();
            double[][] arrAttributes;
            double PredictTime = 0;
            try
            {
                if(modelType== "IPIe")
                { 
                    nError = TfPredictIPIe(new StringBuilder(imagePath), new StringBuilder(settings.StatsFilePath), false, false, 1, false, ref pointerinStruct, ref PredictTime);
                }
                else if(modelType =="BV")
                {     
                    nError = TfPredict(new StringBuilder(imagePath), ref pointerinStruct);
                }
                else if(modelType == "EB")
                {
                    nError = TfPredictEBML(new StringBuilder(imagePath), new StringBuilder(settings.StatsFilePath), false, false, 1, false, ref pointerinStruct, ref PredictTime);
                }
                else if (modelType == "LPC")
                {
                    nError = TfPredictYoloV3(new StringBuilder(imagePath), new StringBuilder(settings.StatsFilePath), false, false, 1, true, ref pointerinStruct, ref PredictTime, true);
                }
                else
                {
                    nError = -1;
                }
            }
            catch (Exception ex) when (ex is AccessViolationException)
            {
                Utilities.LogMessage("MainWindow::PredictClass: " + ex.Message, 0);
                nError = -5;
            }

            if (nError != 0)
            {
                if (modelType == "BV")
                    FreeMarshalStructPtr(ref pointerinStruct);
                else if (modelType == "EB" || modelType == "IPIe" || modelType == "LPC")
                    FreeMarshalStructPtrAladdin(ref pointerinStruct);

                return arrAttributes = null;
            }

            try
            {
                arrAttributes = new double[pointerinStruct.outArray_length][];
                IntPtr[] outputArrayPtrArray = new IntPtr[pointerinStruct.outArray_length];
                Marshal.Copy(pointerinStruct.array, outputArrayPtrArray, 0, pointerinStruct.outArray_length);

                for (int i = 0; i < outputArrayPtrArray.Length; i++)
                {
                    arrAttributes[i] = new double[pointerinStruct.inArray_length];
                    Marshal.Copy(outputArrayPtrArray[i], arrAttributes[i], 0, pointerinStruct.inArray_length);
                }

                if (modelType == "BV")
                    FreeMarshalStructPtr(ref pointerinStruct);
                else if (modelType == "EB" || modelType == "IPIe" || modelType == "LPC")
                    FreeMarshalStructPtrAladdin(ref pointerinStruct);

                return arrAttributes;
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("Error while Marshal copy in PredictImageClass : " + ex.Message, 1);
                return arrAttributes = null;
            }
        }

        private int _autoPilotTotalImages = 0;
        public int AutoPilotTotalImages
        {
            get
            {
                return _autoPilotTotalImages;
            }

            set
            {
                _autoPilotTotalImages = value;
                NotifyPropertyChanged("AutoPilotTotalImages");
            }
        }

        private int _autoPilotNonProcImages = 0;
        public int AutoPilotNonProcImages
        {
            get
            {
                return _autoPilotNonProcImages;
            }

            set
            {
                _autoPilotNonProcImages = value;
                NotifyPropertyChanged("AutoPilotNonProcImages");
                NotifyPropertyChanged("AutoPilotProcessedImages");
            }
        }

        public int AutoPilotProcessedImages
        {
            get
            {
                if (AutoPilotTotalImages > AutoPilotNonProcImages)
                    return AutoPilotTotalImages - AutoPilotNonProcImages;
                else
                    return 0;
            }

            set
            {
                NotifyPropertyChanged("AutoPilotProcessedImages");
            }
        }

        private Visibility _IsVisibleAutoLabellerBVStats = Visibility.Collapsed;
        public Visibility IsVisibleAutoLabellerBVStats
        {
            get
            {
                if (AutoPilotProcessedImages > 0 && settings.dictProjectList[settings.CurrentProject].Contains("BV"))
                    borderAutoLabellerStat.BorderThickness = new Thickness(0, 0, 0, 1);                    
                else
                    borderAutoLabellerStat.BorderThickness = new Thickness(0);

                return _IsVisibleAutoLabellerBVStats;
            }

            set
            {
                _IsVisibleAutoLabellerBVStats = value;
                NotifyPropertyChanged("IsVisibleAutoLabellerBVStats");
            }
        }

        private Visibility _IsVisibleAutoLabellerIPIeStats = Visibility.Collapsed;
        public Visibility IsVisibleAutoLabellerIPIeStats
        {
            get
            {
                if (AutoPilotProcessedImages > 0)
                    borderAutoLabellerStat.BorderThickness = new Thickness(0, 0, 0, 1);
                else
                    borderAutoLabellerStat.BorderThickness = new Thickness(0);

                return _IsVisibleAutoLabellerIPIeStats;
            }

            set
            {
                _IsVisibleAutoLabellerIPIeStats = value;
                NotifyPropertyChanged("IsVisibleAutoLabellerIPIeStats");
            }
        }

        public string ChartDisplayName
        {
            get { return settings.dictProjectList[settings.CurrentProject]; }
            set { NotifyPropertyChanged("ChartDisplayName"); }
        }

        private void btnBarChart_Click(object sender, MouseButtonEventArgs e)
        {
            if(btnBarChart.Kind == MaterialDesignThemes.Wpf.PackIconKind.ArrowRight)
            {
                btnBarChart.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowLeft;
                btnStackChart.ToolTip = "OverAll Chart";
                ChartNonStackClass.Visibility = Visibility.Collapsed;
                ChartStackedClass.Visibility = Visibility.Visible;
            }
            else
            {
                btnStackChart.ToolTip = "Stacked Chart";
                btnBarChart.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowRight;
                ChartNonStackClass.Visibility = Visibility.Visible;
                ChartStackedClass.Visibility = Visibility.Collapsed;
            }
        }

        private void btnPieChart_Click(object sender, MouseButtonEventArgs e)
        {

        }

        private void SaveAutoLabelledStatHistory()
        {
            string Workdir = settings.StatsFilePath + @"\GenieSupervisor_WorkStats";
            if (!Directory.Exists(Workdir))
                Directory.CreateDirectory(Workdir);

            string[] StatsFile = Directory.GetFiles(Workdir, "*AutoPilotStat*.bin");
            string serializationFile = System.IO.Path.Combine(Workdir, "AutoPilotStat_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");

            using (MemoryStream stream = new MemoryStream())
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, settings.CurrentProject);
                bformatter.Serialize(stream, dtLastAutoPilotTime);
                bformatter.Serialize(stream, AutoPilotTotalImages);
                bformatter.Serialize(stream, AutoPilotNonProcImages);

                bformatter.Serialize(stream, ListClassFolderStat.Count);
                for (int count = 0; count < ListClassFolderStat.Count; count++)
                {
                    ClassFolderStat curClassFolder = ListClassFolderStat[count] as ClassFolderStat;
                    bformatter.Serialize(stream, curClassFolder);
                }

                //Delete the old file
                if (StatsFile.Length > 0)
                {
                    foreach (string file in StatsFile)
                        File.Delete(file);
                }

                //Save to new file
                Stream FileStream = File.Open(serializationFile, FileMode.Create);
                stream.WriteTo(FileStream);
                FileStream.Close();
            }
        }

        private void LoadAutoLabelledStatHistory()
        {
            try
            {
                string Workdir = settings.StatsFilePath + @"\GenieSupervisor_WorkStats";
                string[] StatsFile = Directory.GetFiles(Workdir, "*AutoPilotStat*.bin");
                if (StatsFile.Length == 0)
                    return;

                string deSerializFile = System.IO.Path.Combine(Workdir, StatsFile[0]);
                var converter = new System.Windows.Media.BrushConverter();
                string curProject = "";
                using (Stream stream = File.Open(deSerializFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    curProject = (string)bformatter.Deserialize(stream);
                    dtLastAutoPilotTime = (DateTime)bformatter.Deserialize(stream);
                    AutoPilotTotalImages = (int)bformatter.Deserialize(stream);
                    AutoPilotNonProcImages = (int)bformatter.Deserialize(stream);

                    int nTotalCount = (int)bformatter.Deserialize(stream);
                    for (int count = 0; count < nTotalCount; count++)
                    {
                        ClassFolderStat curClassFolder = (ClassFolderStat)bformatter.Deserialize(stream);
                        ListClassFolderStat.Add(curClassFolder);
                    }
                }

                Dispatcher.Invoke(() => {
                    UpdateAutoLabellerClass2DGraph(curProject);
                });
                Utilities.LogMessage("AutoPilot Stat History Loaded");
            }

            catch (Exception ex)
            {
                Utilities.LogMessage("LoadAutoLabelledStatHistory " + ex.Message, 0);
            }
        }
    }
}
