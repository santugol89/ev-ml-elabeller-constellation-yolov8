using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public List<AugmentTypeClass> ListDataAugmentTypeClass = new List<AugmentTypeClass>();

        public void UpdateAugmentationClassData()
        {
            ListDataAugmentTypeClass.Clear();
            string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
            string strTrainDataSetPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\" + settings.trainFolder;
            string strClassNametxtPath = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\classnames.txt";

            if (!Directory.Exists(strTrainDataSetPath) || !File.Exists(strClassNametxtPath))
                return;

            string[] arrTrainClassNames = File.ReadAllLines(strClassNametxtPath);

            if (arrTrainClassNames.Length == 0)
                return;

            if(settings.ClassType == EnumClassType.Segregation)
            {
                Dictionary<string, List<string[]>> dictClassLines = new Dictionary<string, List<string[]>>();
                foreach (KeyValuePair<int,string> keyValue in settings.dictEVSupervisorClass)
                {
                    string curClass = keyValue.Value;
                    string ClassAlias = curClass.Split('(', ')').Length > 1 ? curClass.Split('(', ')')[1] : curClass.Split('(', ')')[0];
                    string strClassName = curClass.Split('(', ')').Length > 0 ? curClass.Split('(', ')')[0] : "";
                    if (!arrTrainClassNames.Contains(strClassName))
                        continue;

                    string strClassID = arrTrainClassNames.ToList().IndexOf(strClassName).ToString();
                    string curClassPath = Path.Combine(strTrainDataSetPath, strClassName);
                    if (!Directory.Exists(curClassPath))
                        continue;

                    List<string[]> listImageFiles = Directory.GetFiles(curClassPath, "*.*", SearchOption.TopDirectoryOnly).Select(item => new string[] { Path.GetFileName(item) }).ToList();

                    ClassStats curClassStat = new ClassStats();
                    curClassStat.AliasName = ClassAlias;
                    curClassStat.ClassName = strClassName;
                    curClassStat.ClassID = strClassID;
                    curClassStat.Count = listImageFiles.Count;

                    ListDataAugmentTypeClass.Add(new AugmentTypeClass(curClassStat)
                    {
                        ListClassAttributes = listImageFiles
                    });
                }
            }
            else
            {
                string trainCSVPath = Path.Combine(strTrainDataSetPath, "train.csv");
                if (!File.Exists(trainCSVPath))
                    return;

                Dictionary<string, List<string[]>> dictClassLines = new Dictionary<string, List<string[]>>();
                List<string> listCSVLines = File.ReadAllLines(trainCSVPath).ToList();
                for(int i = 0; i < listCSVLines.Count; i++)
                {
                    string[] lineSplit = Regex.Split(listCSVLines[i], @"(?<!,[^[]+\{[^}]+),");
                    lineSplit = lineSplit.Select(item => Regex.Replace(item, @"[""{}]", "")).ToArray();
                    if (!IsValidCSVLine(lineSplit) || IsHeaderLine(listCSVLines[i]))
                        continue;

                    if (lineSplit.Length < 4)
                        continue;

                    string ClassID = Regex.Match(lineSplit[3], @"\b[:]\s*[A-Za-z]+").ToString().Replace(":", "").Trim();
                    if (!dictClassLines.ContainsKey(ClassID))
                    {
                        dictClassLines[ClassID] = new List<string[]>();                       
                    }
                    dictClassLines[ClassID].Add(lineSplit);
                }

                foreach (KeyValuePair<int, string> keyValue in settings.dictEVSupervisorClass)
                {
                    string curClass = keyValue.Value;
                    string ClassAlias = curClass.Split('(', ')').Length > 1 ? curClass.Split('(', ')')[1] : curClass.Split('(', ')')[0];
                    string strClassName = curClass.Split('(', ')').Length > 0 ? curClass.Split('(', ')')[0] : "";

                    if (!dictClassLines.ContainsKey(ClassAlias))
                        continue;

                    string strClassID = arrTrainClassNames.ToList().IndexOf(ClassAlias).ToString();
                    List<string[]> listClassAttributes = dictClassLines[ClassAlias];
                    ClassStats curClassStat = new ClassStats();
                    curClassStat.AliasName = ClassAlias;
                    curClassStat.ClassName = strClassName;
                    curClassStat.ClassID = strClassID;
                    curClassStat.Count = listClassAttributes.Count;

                    ListDataAugmentTypeClass.Add(new AugmentTypeClass(curClassStat)
                    {
                        ListClassAttributes = listClassAttributes
                    });
                }
            }

            SaveAugmentationStatHistory();
        }

        public void SaveAugmentationStatHistory()
        {
            string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
            if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                return;
            string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";

            if (!Directory.Exists(Workdir))
                Directory.CreateDirectory(Workdir);

            string[] StatsFile = Directory.GetFiles(Workdir, "*AugmentStat*.bin");
            string serializationFile = System.IO.Path.Combine(Workdir, "AugmentStat_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".bin");

            using (MemoryStream stream = new MemoryStream())
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, ListDataAugmentTypeClass.Count);
                for (int count = 0; count < ListDataAugmentTypeClass.Count; count++)
                {
                    AugmentTypeClass curAugmentType = ListDataAugmentTypeClass[count] as AugmentTypeClass;
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.AliasName);
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.ClassName);
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.ClassID);
                    bformatter.Serialize(stream, curAugmentType.AugmentClassStat.Count);
                    bformatter.Serialize(stream, curAugmentType.AugmentStatCount);
                    bformatter.Serialize(stream, curAugmentType.ListClassAttributes);
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

        private void LoadAugmentationStatHistory()
        {
            try
            {
                ListDataAugmentTypeClass.Clear();
                string strProjectname = settings.dictProjectList.ContainsKey(settings.CurrentProject) ? settings.dictProjectList[settings.CurrentProject] : "";
                if (string.IsNullOrEmpty(strProjectname) || string.IsNullOrEmpty(settings.Architecture))
                    return;
                string Workdir = ConfigFilePath + @"Admin\" + strProjectname + @"\" + settings.Architecture + @"\SavedWork";
                if (!Directory.Exists(Workdir))
                    Directory.CreateDirectory(Workdir);

                string[] StatsFile = Directory.GetFiles(Workdir, "*AugmentStat*.bin");
                if (StatsFile.Length == 0)
                    return;

                string deSerializFile = System.IO.Path.Combine(Workdir, StatsFile[0]);
                var converter = new System.Windows.Media.BrushConverter();

                using (Stream stream = File.Open(deSerializFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    int nAugmentCount = (int)bformatter.Deserialize(stream);
                    for (int count = 0; count < nAugmentCount; count++)
                    {
                        ClassStats curClassStat = new ClassStats();
                        curClassStat.AliasName = (string)bformatter.Deserialize(stream);
                        curClassStat.ClassName = (string)bformatter.Deserialize(stream);
                        curClassStat.ClassID = (string)bformatter.Deserialize(stream);
                        curClassStat.Count = (int)bformatter.Deserialize(stream);

                        AugmentTypeClass curAugmentType = new AugmentTypeClass(curClassStat);
                        curAugmentType.AugmentStatCount = (int)bformatter.Deserialize(stream);
                        curAugmentType.ListClassAttributes = (List<string[]>)bformatter.Deserialize(stream);
                        ListDataAugmentTypeClass.Add(curAugmentType);
                    }
                }

                Utilities.LogMessage("Saved Augmented Stats Loaded");
            }

            catch (Exception ex)
            {
                Utilities.LogMessage("LoadLastAugmentedStatHistory " + ex.Message, 0);
            }
        }

        public PngBitmapEncoder RenderBitmapImage(FrameworkElement augmentCanvas)
        {
            //// Save current canvas transform
            //Transform transform = augmentCanvas.LayoutTransform;
            //// reset current transform (in case it is scaled or rotated)
            //augmentCanvas.LayoutTransform = null;

            // Get the size of canvas
            Size size = new Size(augmentCanvas.Width, augmentCanvas.Height);
            //Measure and arrange the surface
            augmentCanvas.Measure(size);
            augmentCanvas.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, System.Windows.Media.PixelFormats.Pbgra32);
            renderBitmap.Render(augmentCanvas);

            ////Restore previously saved layout
            //augmentCanvas.LayoutTransform = transform;

            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(renderBitmap));

            return png;
        }

        public BmpBitmapEncoder RenderBmpBitmapImage(FrameworkElement augmentCanvas)
        {
            //// Save current canvas transform
            //Transform transform = augmentCanvas.LayoutTransform;
            //// reset current transform (in case it is scaled or rotated)
            //augmentCanvas.LayoutTransform = null;

            // Get the size of canvas
            Size size = new Size(augmentCanvas.Width, augmentCanvas.Height);
            //Measure and arrange the surface
            augmentCanvas.Measure(size);
            augmentCanvas.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, System.Windows.Media.PixelFormats.Pbgra32);
            renderBitmap.Render(augmentCanvas);

            ////Restore previously saved layout
            //augmentCanvas.LayoutTransform = transform;

            FormatConvertedBitmap gray8Image = new FormatConvertedBitmap();
            gray8Image.BeginInit();
            gray8Image.Source = renderBitmap;
            gray8Image.DestinationFormat = System.Windows.Media.PixelFormats.Bgr24; // Convert to 8-bit grayscale
            gray8Image.EndInit();

            BmpBitmapEncoder png = new BmpBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(gray8Image));

            renderBitmap = null;
            gray8Image = null;
            GC.Collect();
            return png;
        }

        public Canvas GetAugmentCanvasForImage(BitmapImage bmpImage, Image augmentImage)
        {
            Canvas AugmentCanvas = new Canvas();
            AugmentCanvas.Height = bmpImage.PixelHeight;
            AugmentCanvas.Width = bmpImage.PixelWidth;
            augmentImage.Height = bmpImage.PixelHeight;
            augmentImage.Width = bmpImage.PixelWidth;
            Canvas.SetLeft(augmentImage, 0);
            Canvas.SetTop(augmentImage, 0);

            return AugmentCanvas;
        }

        public Canvas GetAugmentCanvas(BitmapImage bmpImage, System.Windows.Shapes.Shape shapeROI, Image augmentImage)
        {
            Canvas AugmentCanvas = new Canvas();
            AugmentCanvas.Height = bmpImage.PixelHeight;
            AugmentCanvas.Width = bmpImage.PixelWidth;
            augmentImage.Height = bmpImage.PixelHeight;
            augmentImage.Width = bmpImage.PixelWidth;
            Canvas.SetLeft(augmentImage, 0);
            Canvas.SetTop(augmentImage, 0);
            AugmentCanvas.Children.Add(shapeROI);

            return AugmentCanvas;
        }
    }
}
