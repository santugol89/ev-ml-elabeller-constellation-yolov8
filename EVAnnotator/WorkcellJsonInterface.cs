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
        public List<WorkCellJsonRoot> ListWorkcellJsonRoot;

        public bool LoadProcessedImageFromWorkCellJSON()
        {
            try
            {
                string jsonLines = "";
                ListWorkcellJsonRoot = new List<WorkCellJsonRoot>();
                for (int index = 0; index < settings.ImportFilePath.Length; index++)
                {
                    using (StreamReader reader = new StreamReader(System.IO.File.OpenRead(settings.ImportFilePath[index])))
                    {
                        jsonLines = reader.ReadToEnd();
                        WorkCellJsonRoot cuWorkCellJsonRoot = JsonConvert.DeserializeObject<WorkCellJsonRoot>(jsonLines);
                        ListWorkcellJsonRoot.Add(cuWorkCellJsonRoot);

                        int nRecordCount = cuWorkCellJsonRoot.Lenses.Count;

                        Dispatcher.Invoke(() => progressBar.pbStatus.Maximum = nRecordCount);
                        for (int cnt = 0; cnt < nRecordCount; cnt++)
                        {
                            Dispatcher.Invoke(() => progressBar.pbStatus.Value = cnt);

                            WorkCellLenses curWorkcellLens = cuWorkCellJsonRoot.Lenses[cnt];

                            for(int i = 0; i < curWorkcellLens.AvailablePictures.Count; i++)
                            {
                                string strImageName = curWorkcellLens.AvailablePictures[i].Trim();
                                ImageListBox curImageBox = ProcessedImageBox.Find(item => item.ImageBoxName == strImageName);
                                if (curImageBox == null)
                                {
                                    curImageBox = new ImageListBox(strImageName);
                                    ProcessedImageBox.Add(curImageBox);
                                }

                                foreach(WorkCellUserDefects curUserDefect in curWorkcellLens.UserDefects)
                                {
                                    ImageClass curImageclass = new ImageClass("", "");
                                    curImageclass.Shape = EnumSelectedShape.Circle;
                                    curImageclass.ClassAlias = "";
                                    curImageclass.XCoordinate = (double)curUserDefect.X;
                                    curImageclass.YCoordinate = (double)curUserDefect.Y;
                                    curImageclass.Width = (double)curUserDefect.RX;
                                    curImageclass.Height = (double)curUserDefect.RY;
                                    curImageclass.Rotation = curUserDefect.Rotation;
                                    curImageclass.All_Points_X = new List<double>();
                                    curImageclass.All_Points_Y = new List<double>();
                                    curImageclass.ShapeCoordinates = "";
                                    curImageclass.Reviewed = false;
                                    curImageclass.ImportDatasheetName = settings.ImportFilePath[index];
                                    curImageBox.ListImageClass.Add(curImageclass);
                                }
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
                MessageBox.Show("Invalid Workcell JSON File", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                return false;
            }
        }
    }

    public class WorkCellJsonRoot
    {
        public string Id { get; set; }
        public List<WorkCellLenses> Lenses { get; set; }

        public WorkCellJsonRoot()
        {
            Lenses = new List<WorkCellLenses>();
        }
    }

    public class WorkCellLenses
    {
        public int Index { get; set; }
        public string State { get; set; }
        public string DisplayName { get; set; }
        public List<WorkCellUserDefects> UserDefects { get; set; }
        public string LabeledAt { get; set; }
        public string LabeledBy { get; set; }
        public bool LabeledWithTemplateLens { get; set; }
        public bool PicturesPresent { get; set; }
        public List<System.String> AvailablePictures { get; set; }

    }

    public class WorkCellUserDefects
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int RX { get; set; }
        public int RY { get; set; }
        public int Rotation { get; set; }
        public string Type { get; set; }
        public string Variant { get; set; }
    }
}
