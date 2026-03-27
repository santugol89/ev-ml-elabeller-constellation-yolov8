using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        
        public bool LoadProcessedImageFromCocoModelJSON()
        {
            try
            {
                string jsonLines = "";
                for (int index = 0; index < settings.ImportFilePath.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(settings.ImportFilePath[index])))
                    {
                        jsonLines = reader.ReadToEnd();
                        CocoModelJsonRoot curCocoModelJsonRoot = JsonConvert.DeserializeObject<CocoModelJsonRoot>(jsonLines);

                        int nRecordCount = curCocoModelJsonRoot.images.Count;

                        Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = nRecordCount);
                        for (int cnt = 0; cnt < nRecordCount; cnt++)
                        {
                            Dispatcher.Invoke(() => progressBar.pbStatus.Value = cnt);

                            CocoModelImages curCocoModelImages = curCocoModelJsonRoot.images[cnt];
                            string strImageName = Path.GetFileName(curCocoModelImages.file_name.Trim());
                            ImageListBox curImageBox = ProcessedImageBox.Find(item => item.ImageBoxName == strImageName);
                            if (curImageBox == null)
                            {
                                curImageBox = new ImageListBox(strImageName);
                                curImageBox.ImageHeight = curCocoModelImages.height;
                                curImageBox.Imagewidth = curCocoModelImages.width;
                                ProcessedImageBox.Add(curImageBox);
                            }

                            List<CocoModelAnnotations> listAnnotations = curCocoModelJsonRoot.annotations.Where(item => item.image_id == curCocoModelImages.id).ToList();
                            for (int i = 0; i < listAnnotations.Count; i++)
                            {
                                CocoModelAnnotations curAnnotations = listAnnotations[i];
                                CocoModelCategories curCategory = curCocoModelJsonRoot.categories.FirstOrDefault(item => item.id == curAnnotations.category_id);
                                if (curCategory == null)
                                    continue;

                                var strTemp = settings.dictEVSupervisorClass.Values.Where(temp => temp.Contains(curCategory.name)).FirstOrDefault();
                                string classAlias = "";
                                if (!string.IsNullOrEmpty(strTemp))
                                {
                                    classAlias = strTemp.Split('(').Length > 1 ? strTemp.Split('(')[1].Replace(")", "") : "";
                                }

                                string className = classAlias != ""? curCategory.name + "(" + classAlias + ")" : curCategory.name;
                                ImageClass curImageclass = new ImageClass(curCategory.id.ToString(), className);
                                curImageclass.ClassAlias = classAlias;

                                if (curAnnotations.segmentation.Count == 0)
                                    continue;

                                ClassFolderStat curclassFolder = ListClassFolderStat.FirstOrDefault(item => item.ClassAliasName.ToUpper() == classAlias.ToUpper() && item.ImportDatasheetName == settings.ImportFilePath[index].Trim());
                                if (curclassFolder == null)
                                {
                                    ListClassFolderStat.Add(new ClassFolderStat
                                    {
                                        ImportDatasheetName = settings.ImportFilePath[index].Trim(),
                                        ClassCount = 1,
                                        ClassAliasName = classAlias,
                                        ClassID = curImageclass.ClassIndex,
                                        SingleSpotCount = 0,
                                        PhaseContrastCount = 0
                                    });
                                }
                                else
                                {
                                    curclassFolder.ClassCount++;
                                }

                                curImageclass.XCoordinate = curAnnotations.bbox[0];
                                curImageclass.YCoordinate = curAnnotations.bbox[1];
                                curImageclass.Width = curAnnotations.bbox[2];
                                curImageclass.Height = curAnnotations.bbox[3];
                                if (curAnnotations.segmentation.First().Count > 0)
                                {
                                    int k = 0;
                                    while (k < curAnnotations.segmentation.First().Count)
                                    {
                                        curImageclass.All_Points_X.Add(curAnnotations.segmentation.First()[k]);
                                        k += 2;
                                    }

                                    int m = 1;
                                    while (m < curAnnotations.segmentation.First().Count)
                                    {
                                        curImageclass.All_Points_Y.Add(curAnnotations.segmentation.First()[m]);
                                        m += 2;
                                    }

                                    curImageclass.Shape = EnumSelectedShape.Polyline;
                                    curImageclass.ShapeCoordinates = "{\"name\":\"polyline\", \"all_points_x\": [" +
                                                                    String.Join(", ", curImageclass.All_Points_X) + "], \"all_points_y\": [" + String.Join(", ", curImageclass.All_Points_Y) + "] }";

                                }
                                else
                                {
                                    curImageclass.Shape = EnumSelectedShape.Rectangle;
                                    curImageclass.ShapeCoordinates = "{\"name\":\"rect\", \"x\": " + curImageclass.XCoordinate + ", \"y\": " + curImageclass.YCoordinate +
                                                                    ", \"width\": " + curImageclass.Width + ", \"height\": " + curImageclass.Height + " }";
                                }

                                curImageclass.Reviewed = false;
                                curImageclass.ImportDatasheetName = settings.ImportFilePath[index];
                                curImageBox.ListImageClass.Add(curImageclass);
                            }
                        }
                        settings.nImportFileRecordCount[index] = nRecordCount;
                    }
                }
                TotalDataSheet = settings.ImportFilePath.Length;
                TotalRecordFound = settings.nImportFileRecordCount.Sum();
                TotalViolationFound = 0;

                return true;
            }

            catch (Exception ex)
            {
                MessageBox.Show("Invalid COCO Compatible JSON File..!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return false;
            }
        }

        public void ExportCocoModelJsonIntoOutput()
        {
            try
            {
                string strDTNow = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                string strDataPath = settings.CSVExportPath + @"\Output Data\CocoModel JSON";
                if (!Directory.Exists(strDataPath))
                    Directory.CreateDirectory(strDataPath);

                string strRectJsonSavePath = System.IO.Path.Combine(strDataPath, "coco_" + strDTNow + ".json");
                bool bIsDataSave = false;
                this.Dispatcher.Invoke(() =>
                {
                    System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                    
                    saveFileDialog.InitialDirectory = strDataPath;
                    saveFileDialog.Filter = "json file|*.json";
                    saveFileDialog.FileName = "coco_" + strDTNow + ".json";

                    System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        strRectJsonSavePath = saveFileDialog.FileName;
                        bIsDataSave = true;
                    }
                });

                if(!bIsDataSave)
                {
                    OnWorkerMethodComplete("Complete");
                    return;
                }

                int noLicense = 99;
                CocoModelJsonRoot curCocoModelJsonRoot = new CocoModelJsonRoot();
                CocoModelInfo curCocoInfo = new CocoModelInfo
                {
                    description = "LS3 BV Dataset",
                    url = "",
                    version = "",
                    year = 2022,
                    contributor = "EV",
                    date_created = DateTime.Now.Date.ToString("yyyy/mm/dd")
                };
                curCocoModelJsonRoot.info = curCocoInfo;

                CocoModelLicence curCocoLicence = new CocoModelLicence
                {
                    id = noLicense,
                    url = "",
                    name = "EV"
                };
                curCocoModelJsonRoot.licenses.Add(curCocoLicence);

                int nAnnotationid = 0;
                for (int nCount = 0; nCount < ImageMenuList.Count; nCount++)
                {
                    ImageMenu curImage = ImageMenuList[nCount];
                    CocoModelImages curCocoImages = new CocoModelImages
                    {
                        id = nCount,
                        license = noLicense,
                        url = "",
                        file_name = curImage.ImagePath,
                        height = curImage.ImageBox.ImageHeight,
                        width = curImage.ImageBox.Imagewidth,
                        date_captured = DateTime.Now.Date.ToString("yyyy-mm-dd HH:mm:ss")
                    };
                    curCocoModelJsonRoot.images.Add(curCocoImages);

                    for(int img = 0; img < curImage.ImageBox.ListImageClass.Count; img++)
                    {
                        ImageClass curImageclass = curImage.ImageBox.ListImageClass[img] as ImageClass;
                        List<double> listSegments = new List<double>();

                        for(int i = 0; i < curImageclass.All_Points_X.Count; i++)
                        {
                            listSegments.Add(curImageclass.All_Points_X[i]);
                            listSegments.Add(curImageclass.All_Points_Y[i]);
                        }

                        double x, y, x1, y1, width, height;
                        double[] arrBoundBoxes = null;
                        if (curImageclass.All_Points_X.Count > 0)
                        {
                            x = curImageclass.All_Points_X.Min();
                            y = curImageclass.All_Points_Y.Min();
                            x1 = curImageclass.All_Points_X.Max();
                            y1 = curImageclass.All_Points_Y.Max();
                            width = x1 - x;
                            height = y1 - y;
                        }
                        else
                        {
                            x = curImageclass.XCoordinate;
                            y = curImageclass.YCoordinate;
                            width = curImageclass.Width;
                            height = curImageclass.Height;
                        }
                        arrBoundBoxes = new double[4] { x, y, width, height };

                        CocoModelAnnotations curAnnotation = new CocoModelAnnotations
                        {
                            id = nAnnotationid,
                            image_id = nCount,
                            category_id = Convert.ToInt32(curImageclass.ClassIndex),
                            segmentation = new List<List<double>>() {listSegments},
                            area = GetAreaFromPolygon(curImageclass),
                            bbox = arrBoundBoxes,
                            iscrowd = 0
                        };

                        curCocoModelJsonRoot.annotations.Add(curAnnotation);
                    }
                }

                curCocoModelJsonRoot.type = "instances";

                foreach(KeyValuePair<int, string> curClass in settings.dictEVSupervisorClass)
                {
                    CocoModelCategories curCategory = new CocoModelCategories
                    {
                        id = curClass.Key,
                        supercategory = "FEdge - Defects",
                        name = curClass.Value.Split('(', ')').Length > 0 ? curClass.Value.Split('(', ')')[0] : ""
                    };
                    curCocoModelJsonRoot.categories.Add(curCategory);
                }
                
                string output = JsonConvert.SerializeObject(curCocoModelJsonRoot, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(strRectJsonSavePath, output);
                OnWorkerMethodComplete("Complete");
                System.Windows.MessageBox.Show("COCO Compatible JSON has been exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
            catch (Exception ex) when (ex is PathTooLongException || ex is DirectoryNotFoundException)
            {
                OnWorkerMethodComplete("Complete");
                MessageBox.Show("The specified Output Data path, file name, or both are too long..!\nPlease select proper path in settings->Output Data Path..", "Long Path Error",
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }

            catch (System.Exception ex)
            {
                OnWorkerMethodComplete("Complete");
                MessageBox.Show("Something went wrong. Export Failed.!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                Utilities.LogMessage("CocoModelJsonInterface::ExportCocoModelJsonIntoOutput: " + ex.Message, 9);
            }
        }

        public double GetAreaFromPolygon(ImageClass curImageclass)
        {
            int i, j;
            double area = 0;

            for (i = 0; i < curImageclass.All_Points_X.Count; i++)
            {
                j = (i + 1) % curImageclass.All_Points_X.Count;

                area += curImageclass.All_Points_X[i] * curImageclass.All_Points_Y[j];
                area -= curImageclass.All_Points_Y[i] * curImageclass.All_Points_X[j];
            }

            area /= 2;
            return Math.Abs(area);
        }
    }

    public class CocoModelJsonRoot
    {
        public CocoModelInfo info { get; set; }

        public List<CocoModelLicence> licenses { get; set; }

        public List<CocoModelImages> images { get; set; }

        public string type { get; set; }

        public List<CocoModelAnnotations> annotations { get; set; }

        public List<CocoModelCategories> categories { get; set; }


        public CocoModelJsonRoot()
        {
            info = new CocoModelInfo();
            licenses = new List<CocoModelLicence>();
            images = new List<CocoModelImages>();
            annotations = new List<CocoModelAnnotations>();
            categories = new List<CocoModelCategories>();
        }
    }

    public class CocoModelInfo
    {
        public string description { get; set; }
        public string url { get; set; }
        public string version { get; set; }
        public int year { get; set; }
        public string contributor { get; set; }
        public string date_created { get; set; }
   }

    public class CocoModelLicence
    {
        public string url { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }

    public class CocoModelImages
    {
        public int id { get; set; }
        public int license { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string file_name { get; set; }
        public string date_captured { get; set; }
    }


    public class CocoModelAnnotations
    {
        public int id { get; set; }
        public int image_id { get; set; }
        public int category_id { get; set; }
        public List<List<double>> segmentation { get; set; }
        public double area { get; set; }
        public double[] bbox { get; set; }
        public int iscrowd { get; set; }
    }

    public class CocoModelCategories
    {
        public string supercategory { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }
}
