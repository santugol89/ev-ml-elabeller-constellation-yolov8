using IniParser;
using IniParser.Model;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace GenieSupervisor
{
    public class Settings
    {
        MainWindow app;
        public List<string> LoadedImagefiles;      
        public string[] ImportFilePath;
        public int[] nImportFileRecordCount;
        public string[] ImportXMLFilePath;
        public double ZoomLevel;
        public int DIR_MAX_LENGTH = 248;
        public int FILE_MAX_LENGTH = 260;
        public string SinglePhase = ".d";
        public string PhaseContrast = ".h";
        string imagePath;
        public string LoadImagePath { get { return imagePath; } set { imagePath = value; } }
        public string DefaultImageLoadPath;

        string fileCSVPath;
        public string LoadCSVImportPath { get { return fileCSVPath; } set { fileCSVPath = value; } }
        
        public int classCount;

        string classFilePath;
        public string ClassFilePath { get { return classFilePath; } set { classFilePath = value; } }

        string csvExportPath;
        public string CSVExportPath
        {
            get
            {
                return csvExportPath;
            }
            set
            { csvExportPath = value; }
        }

        public Dictionary<int, string> dictEVSupervisorClass;
        public List<string> ListEVSupervisorClassAlias;
        public List<string> ListPassClass;
        public List<string> ListFailClass;

        public int ProjectCount;
        public Dictionary<string, string> dictProjectList;
        public string[] LineList;
        public string CurrentProject;

        public EnumClassType ClassType;
        public string Architecture;
        public string Station;
        public string PatchcoreIlluminationType;

        public string StatsFilePath;
        public bool blnValidationStat;
        public bool bIsValidatewithID { get; set; }
       
        public string ApplicationMode = "Normal";
        public AugmentationTypeConfig CurrentAugmentConfig;

        public string ModelPath;
        public ulong LoadedImageSize = 0;
        public bool bIsLoaded = false;
        public string CSVExportFolder = "Export csv";
        public string valFolder = "val";
        public string trainFolder = "train";
        public string testsetFolder = "test";
        public string sourceFolder = "source";
        public List<string> ListArchitectures = new List<string>();
        public int DefaultRadius = 19;
        public List<string> ListIlluminations = new List<string>();

        public string ClassificationAlias { get; set; } = "Classification";
        public string DetectionAlias { get; set; } = "Object Detection";
        public string SegmentationAlias { get; set; } = "SegmentationV8";
        public string PatchcoreAlias { get; set; } = "Anomaly Detection";

        public Settings(MainWindow app)
        {
            this.app = app;
            StatsFilePath = app.ConfigFilePath + @"EVLabeller\";            
            CurrentAugmentConfig = new AugmentationTypeConfig();
            ReadConfigurationSettings();
            Utilities.LogMessage("Settings loaded");
        }

        public void ReadConfigurationSettings()
        {
            try
            {
                if (StatsFilePath.Substring(StatsFilePath.Length - 1) != "\\")
                    StatsFilePath += "\\";
                if (StatsFilePath != null)
                {
                    System.IO.Directory.CreateDirectory(StatsFilePath);
                }
                else
                {
                    StatsFilePath = @"C:\EVLabeller\";
                    System.IO.Directory.CreateDirectory(StatsFilePath);
                }

                CheckandLoadNewClassFile();
                string strIniFile = Path.Combine(StatsFilePath + @"Preferences.ini");
                IniFile iniRead = new IniFile(strIniFile);
                LoadImagePath = iniRead.ReadValue("Settings", "LoadImagePath", "");
                LoadCSVImportPath = iniRead.ReadValue("Settings", "LoadCSVFileImport", "");
                ClassFilePath = iniRead.ReadValue("Settings", "ClassFilePath", "");
                CSVExportPath = iniRead.ReadValue("Settings", "CSVFileExportPath", StatsFilePath);
                if (!Directory.Exists(CSVExportPath))
                {
                    CSVExportPath = StatsFilePath;
                }
                app.IsEnableRectangle = iniRead.ReadValue("Settings", "EnableRectangle", true);
                app.IsEnableCircle = iniRead.ReadValue("Settings", "EnableCircle", false);
                app.IsEnablePoly = iniRead.ReadValue("Settings", "EnablePolyline", false);
                ZoomLevel = iniRead.ReadValue("Settings", "DefaultZoomLevel", 0.0);
                blnValidationStat = iniRead.ReadValue("Settings", "ValidationStat", false);
                bIsValidatewithID = iniRead.ReadValue("Settings", "ValidateWithID", true);
                ProjectCount = iniRead.ReadValue("Settings", "Projects", 0);
                CurrentProject = iniRead.ReadValue("Settings", "CurrentProject", "P0");
                DefaultImageLoadPath = LoadImagePath;
                app.IsVisibleShapeQuickPallete = CurrentProject == "P0" ? Visibility.Collapsed : Visibility.Visible;
                ModelPath = iniRead.ReadValue("Settings", "ModelPath", "");
                DefaultRadius = iniRead.ReadValue("Settings", "DefaultRadius", 42);
                bIsLoaded = GetProjectList();
                CurrentProject = GetCurrentProject();

                CurrentAugmentConfig.NoiseValue = iniRead.ReadValue("AugmentConfig", "NoiseValue", 0.5);
                CurrentAugmentConfig.RotateDegree = iniRead.ReadValue("AugmentConfig", "RotateDegree", 90);
                CurrentAugmentConfig.Trans_Coordinate[0] = iniRead.ReadValue("AugmentConfig", "Trans_X", 20);
                CurrentAugmentConfig.Trans_Coordinate[1] = iniRead.ReadValue("AugmentConfig", "Trans_Y", 10);
                CurrentAugmentConfig.BlurRatio = iniRead.ReadValue("AugmentConfig", "BlurRatio", 1.5);

                var parser = new FileIniDataParser();
                IniData data = null;
                if (File.Exists(strIniFile))
                    data = parser.ReadFile(strIniFile);

                if (data != null && data.Sections.ContainsSection("Architecture"))
                {
                    var dictTempInfo = data["Architecture"].ToDictionary(key => key.KeyName, value => value.Value);
                    foreach (KeyValuePair<string, string> keyValuePair in dictTempInfo)
                        ListArchitectures.Add(keyValuePair.Value);
                }
                if (ListArchitectures.Count == 0)
                {
                    ListArchitectures.Add(DetectionAlias);
                    ListArchitectures.Add(PatchcoreAlias);
                }

                if (data != null && data.Sections.ContainsSection("IlluminationTypes"))
                {
                    var dictTempInfo = data["IlluminationTypes"].ToDictionary(key => key.KeyName, value => value.Value);
                    foreach (KeyValuePair<string, string> keyValuePair in dictTempInfo)
                        ListIlluminations.Add(keyValuePair.Value);
                }

                if(ListIlluminations.Count == 0)
                {
                    ListIlluminations.Add("TopMono");
                    ListIlluminations.Add("TopColor");
                    ListIlluminations.Add("SideTB");
                    ListIlluminations.Add("SideLR");
                }
                app.ValidationStatVisibility = blnValidationStat ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                ReadClassFileConfig();
                Utilities.LogMessage("ReadConfiguration in settings loaded", 0);
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("ReadConfigurationSettings: " + ex.Message, 9);
            }
        }

        private string GetCurrentProject()
        {
            if (ClassFilePath == "" || !File.Exists(ClassFilePath))
            {
                string[] arrProjects = Directory.GetFiles(StatsFilePath, "*Genie*.ini", SearchOption.AllDirectories);
                if (arrProjects.Length > 0)
                    ClassFilePath = arrProjects[0];
                else
                    return "";
            }                

            IniFile iniRead = new IniFile(ClassFilePath);
            string projectName = iniRead.ReadValue("ClassInfo", "ProjectName", "");

            string projectKey = dictProjectList.FirstOrDefault(item => item.Value == projectName).Key;
            return projectKey;
        }

        private bool GetProjectList()
        {
            try
            {
                dictProjectList = new Dictionary<string, string>();
                string[] arrProjects = Directory.GetFiles(StatsFilePath, "*Genie*.ini", SearchOption.AllDirectories);
                if (arrProjects.Length > 0)
                {
                    dictProjectList.Add("P0", "NONE");
                    for (int i = 0; i < arrProjects.Length; i++)
                    {
                        IniFile iniRead = new IniFile(arrProjects[i]);
                        string projectKey = iniRead.ReadValue("ClassInfo", "Project", "");
                        string projectName = iniRead.ReadValue("ClassInfo", "ProjectName", "");
                        if (projectName == string.Empty)
                        {
                            if (Path.GetFileNameWithoutExtension(arrProjects[i]).Contains("Genie_BV"))
                                projectName = "LS3 BV";
                            else
                            {
                                string temp = Path.GetFileNameWithoutExtension(arrProjects[i]);
                                projectName = Regex.Split(temp, "Genie_").Length > 1 ? Regex.Split(temp, "Genie_")[1] : "";
                            }
                        }

                        if (!dictProjectList.ContainsValue(projectName))
                        {
                            if(!dictProjectList.ContainsKey(projectKey))
                                dictProjectList.Add(projectKey, projectName);
                            else
                            {
                                int k = arrProjects.Length + 1;
                                projectKey = "P" + arrProjects.Length; 
                                while(dictProjectList.ContainsKey(projectKey))
                                    projectKey = "P" + k++;

                                dictProjectList.Add(projectKey, projectName);
                            }
                        }
                    }

                    dictProjectList = dictProjectList.OrderBy(item => int.Parse(new string(item.Key.Skip(1).ToArray()))).ToDictionary(x => x.Key, x => x.Value);
                    ProjectCount = dictProjectList.Count;
                    Dictionary<string, string> updateDict = new Dictionary<string, string>();
                    for (int k = 0; k < ProjectCount; k++)
                    {
                        KeyValuePair<string, string> keyValue = dictProjectList.ElementAt(k);
                        string newKey = "P" + k;
                        updateDict[newKey] = keyValue.Value;
                    }
                    dictProjectList = updateDict;
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("Error while loading projects : " + ex.Message.ToString());
                MessageBox.Show("Error while loading some of projects from settings.\nPlease check proper project files present in " + StatsFilePath + " folder.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                return false;
            }
        }

        private void CheckandLoadNewClassFile()
        {
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string[] classFiles = Directory.GetFiles(appPath, "*Genie*.ini", SearchOption.AllDirectories);
            if(classFiles.Length > 0)
            {
                MessageBoxResult result = MessageBox.Show("New Class file detected. Do you want to load new class file?", "Configuration", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question,MessageBoxResult.Yes,MessageBoxOptions.ServiceNotification);

                if(result == System.Windows.MessageBoxResult.Yes)
                {
                    string[] temp = Directory.GetFiles(StatsFilePath, "*Genie*.ini", SearchOption.AllDirectories);
                    for (int i = 0; i < temp.Length; i++)
                        System.IO.File.Delete(temp[i]);

                    int k = 0;
                    while (k < classFiles.Length)
                    {
                        string destFile = StatsFilePath + Path.GetFileName(classFiles[k]);
                        System.IO.File.Copy(classFiles[k], destFile, true);
                        System.IO.File.Delete(classFiles[k]);
                        k++;
                    }
                }
            }
        }

        public void ReadClassFileConfig()
        {
            try
            {
                IniFile iniRead1 = new IniFile(ClassFilePath);
                classCount = int.Parse(iniRead1.ReadValue("ClassInfo", "Classes ", "0"));
                string strTemp = iniRead1.ReadValue("Annotation", "Type ", "None");
                ClassType = (EnumClassType)Enum.Parse(typeof(EnumClassType), strTemp, true);
                Architecture = iniRead1.ReadValue("ClassInfo", "Architecture ", "");
                Station = iniRead1.ReadValue("ClassInfo", "Station ", "");
                PatchcoreIlluminationType = iniRead1.ReadValue("ClassInfo", "IlluminationType", "");

                string strClassName;
                dictEVSupervisorClass = new Dictionary<int, string>();
                ListEVSupervisorClassAlias = new List<string>();
                ListPassClass = new List<string>();
                ListFailClass = new List<string>();
                app.ListModifiedClass = new List<ModifiedClass>();
                for (int i = 0, j = 0; i < classCount; i++, j++)
                {
                    //If class index present in random number & it is to limit loop for 200
                    if (j > 200)
                        break;
                    strClassName = iniRead1.ReadValue("Class", String.Format("C{0}", j), "");
                    if (String.IsNullOrEmpty(strClassName))
                    {
                        i--;
                        continue;
                    }
                    dictEVSupervisorClass[j] = strClassName;

                    string ClassAlias = strClassName.Split('(', ')').Length > 1 ? strClassName.Split('(', ')')[1] : strClassName.Split('(', ')')[0];
                    app.ListModifiedClass.Add(new ModifiedClass(j.ToString(), ClassAlias));
                    ListEVSupervisorClassAlias.Add(ClassAlias);

                    string strPass = iniRead1.ReadValue("Pass", String.Format("C{0}", j), "");
                    if (!String.IsNullOrEmpty(strPass))
                        ListPassClass.Add(strPass);

                    string strFail = iniRead1.ReadValue("Fail", String.Format("C{0}", j), "");
                    if (!String.IsNullOrEmpty(strFail))
                        ListFailClass.Add(strFail);
                }

                int LineCount = int.Parse(iniRead1.ReadValue("ClassInfo", "Lines ", "0"));
                LineList = new string[LineCount];
                for (int n = 0; n < LineCount; n++)
                    LineList[n] = iniRead1.ReadValue("Line", String.Format("L{0}", n + 1), "");
                Utilities.LogMessage("Class loaded");

                string strProjectname = iniRead1.ReadValue("ClassInfo", "ProjectName ", "");
                if (strProjectname != "" && Architecture != "")
                {
                    LoadCSVImportPath = app.ConfigFilePath + @"Admin\" + strProjectname + @"\" + Architecture + @"\" + CSVExportFolder;
                    if (!Directory.Exists(LoadCSVImportPath))
                        Directory.CreateDirectory(LoadCSVImportPath);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("ReadClassFileConfig: " + ex.Message, 9);
            }
        }

        public void WriteConfigSettings()
        {
            if (File.Exists(StatsFilePath + @"Preferences.ini"))
                File.Delete(StatsFilePath + @"Preferences.ini");

            IniFile iniWrite = new IniFile(StatsFilePath + @"Preferences.ini");
            iniWrite.WriteValue("Settings", "LoadImagePath", LoadImagePath);
            iniWrite.WriteValue("Settings", "ClassFilePath", ClassFilePath);
            iniWrite.WriteValue("Settings", "LoadCSVFileImport", LoadCSVImportPath);
            iniWrite.WriteValue("Settings", "CSVFileExportPath", CSVExportPath);
            iniWrite.WriteValue("Settings", "EnableRectangle", app.IsEnableRectangle);
            iniWrite.WriteValue("Settings", "EnableCircle", app.IsEnableCircle);
            iniWrite.WriteValue("Settings", "EnablePolyline", app.IsEnablePoly);
            iniWrite.WriteValue("Settings", "DefaultZoomLevel", ZoomLevel);
            iniWrite.WriteValue("Settings", "ValidationStat", blnValidationStat);
            iniWrite.WriteValue("Settings", "ValidateWithID", bIsValidatewithID);
            iniWrite.WriteValue("Settings", "CurrentProject", CurrentProject);
            iniWrite.WriteValue("Settings", "Projects", dictProjectList.Count);
            iniWrite.WriteValue("Settings", "ModelPath", ModelPath);
            iniWrite.WriteValue("Settings", "DefaultRadius", DefaultRadius);

            for (int i = 0; i < dictProjectList.Count; i++)
            {
                try
                {
                    iniWrite.WriteValue("Projects", String.Format("P{0}", i), dictProjectList["P" + i]);
                }
                catch { }
            }

            iniWrite.WriteValue("AugmentConfig", "NoiseValue", CurrentAugmentConfig.NoiseValue);
            iniWrite.WriteValue("AugmentConfig", "RotateDegree", CurrentAugmentConfig.RotateDegree);
            iniWrite.WriteValue("AugmentConfig", "Trans_X", CurrentAugmentConfig.Trans_Coordinate[0]);
            iniWrite.WriteValue("AugmentConfig", "Trans_Y", CurrentAugmentConfig.Trans_Coordinate[1]);
            iniWrite.WriteValue("AugmentConfig", "BlurRatio", CurrentAugmentConfig.BlurRatio);

            for(int i = 0; i < ListArchitectures.Count; i++)
                iniWrite.WriteValue("Architecture", String.Format("A{0}", i), ListArchitectures[i]);

            for(int i = 0; i < ListIlluminations.Count; i++)
                iniWrite.WriteValue("IlluminationTypes", String.Format("{0}", i), ListIlluminations[i]);
        }

        public bool CheckFileAccess(string strFileFullPath)
        {
            try
            {
                if (File.Exists(strFileFullPath) == false)
                    return false;

                using (FileStream stream = File.Open(strFileFullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (stream != null)
                        stream.Close();
                    return false;
                }
            }
            catch (Exception e)
            {
                return true;
            }
        }
    }

    public class AugmentationTypeConfig
    {
        public double noiseValue = 0.5;
        public double NoiseValue
        {
            get { return noiseValue; }
            set { noiseValue = value; }
        }

        public int rotateDegree = 90;
        public int RotateDegree
        {
            get { return rotateDegree; }
            set { rotateDegree = value; }
        }

        public double[] trans_Coordinate = new double[2]{20,10};
        public double[] Trans_Coordinate
        {
            get { return trans_Coordinate; }
            set { trans_Coordinate = value; }
        }

        public double blurRatio = 1.5;
        public double BlurRatio
        {
            get { return blurRatio; }
            set { blurRatio = value; }
        }
    }
}
