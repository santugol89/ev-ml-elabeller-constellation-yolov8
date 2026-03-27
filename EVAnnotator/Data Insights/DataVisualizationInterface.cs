using GenieSupervisor.Data_Insights;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        List<UserControl> ListDataInsightView = new List<UserControl>();
        UControlClasswiseImageCountGraph pageClasswiseImageGraph = new UControlClasswiseImageCountGraph();
        UControlClasswiseLabelCountGraph pageClasswiseLabelGraph = new UControlClasswiseLabelCountGraph();
        UControlLabellingStatsGraph pageLabellingStatsGraph = new UControlLabellingStatsGraph();
        UControlTimeAnalysisGraph pageTimeAnalysisGrpaph = new UControlTimeAnalysisGraph();
        public int CurIndex = 0;
        public List<ClassWiseTimeAnalysis> ListClassWiseTimeAnalyses = new List<ClassWiseTimeAnalysis>();

        public void InitializeDataInsightView()
        {
            CurIndex = 0;
            ListDataInsightView.Clear();
            gridDataInsightView.Children.Clear();
            ListDataInsightView.Add(pageClasswiseImageGraph);
            if(settings.ClassType != EnumClassType.Segregation)
                ListDataInsightView.Add(pageClasswiseLabelGraph);
            ListDataInsightView.Add(pageLabellingStatsGraph);
            ListDataInsightView.Add(pageTimeAnalysisGrpaph);
            gridDataInsightView.Children.Add(ListDataInsightView[CurIndex]);
            borderNextView.Visibility = Visibility.Visible;
            borderPreView.Visibility = Visibility.Collapsed;
            pageTimeAnalysisGrpaph.app = this;

            LoadAllVisualizationGraphs();
            Utilities.LogMessage("Data insights loaded successfully");
        }

        public void LoadAllVisualizationGraphs()
        {
            LoadClasswiseImageCountData();
            LoadClasswiseLabelCountData();
            LoadLabellingStatisticsData();
        }

        public void ClearGraphs()
        {
            DistByImageCount = null;
            DistByLabelCount = null;
            DistByLabelStat = null;
            DistByTimeWiseClass = null;
            pageTimeAnalysisGrpaph.cmbDefectFilter.ItemsSource = null;
            pageTimeAnalysisGrpaph.lvChangeofPercent.ItemsSource = null;
        }

        private void LoadClasswiseImageCountData()
        {
            List<ClassWiseCount> listClassWiseImageCount = new List<ClassWiseCount>();
            Dictionary<string, string> tempDictList = new Dictionary<string, string>();
            //foreach (var curItem in settings.dictEVSupervisorClass)
            //{
            //    string className = curItem.Value.Split('(', ')').Length > 0 ? curItem.Value.Split('(', ')')[0] : "Unknown Class";
            //    string classAlias = curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0];
            //    tempDictList.Add(classAlias, className);
            //}

            //foreach (ClassFolderStat curClassFolder in ListClassFolderStat)
            //{
            //    string classAlias = curClassFolder.ClassAliasName;
            //    string className = tempDictList.ContainsKey(classAlias) ? tempDictList[classAlias] : "Unknown Class";
            //    int count = ImageMenuList.Where(item =>item.ImageBox.ListImageClass.Any(i => i.ClassAlias != null && i.ClassAlias.ToUpper() == classAlias.ToUpper())).Count();

            //    ClassWiseCount curClassWiseCount = listClassWiseImageCount.FirstOrDefault(item => item.ClassAlias.ToUpper() == classAlias.ToUpper());
            //    if(curClassWiseCount == null)
            //    {
            //        listClassWiseImageCount.Add(new ClassWiseCount()
            //        {
            //            ClassAlias = classAlias,
            //            ClassName = className,
            //            ClassCount = count
            //        });
            //    }                              
            //}

            foreach (var curItem in settings.dictEVSupervisorClass)
            {
                string className = curItem.Value.Split('(', ')').Length > 0 ? curItem.Value.Split('(', ')')[0] : "Unknown Class";
                string classAlias = curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0];
                int count = ImageMenuList.Where(item => item.ImageBox.ListImageClass.Any(i => i.ClassAlias != null && i.ClassAlias.ToUpper() == classAlias.ToUpper())).Count();

                ClassWiseCount curClassWiseCount = listClassWiseImageCount.FirstOrDefault(item => item.ClassAlias.ToUpper() == classAlias.ToUpper());
                if (curClassWiseCount == null)
                {
                    listClassWiseImageCount.Add(new ClassWiseCount()
                    {
                        ClassAlias = classAlias,
                        ClassName = className,
                        ClassCount = count
                    });
                }
            }
            var listOrderedImageCountList = listClassWiseImageCount.OrderByDescending(item => item.ClassCount).ToList();
            int nTotal = listOrderedImageCountList.Sum(item => item.ClassCount);
            int nTotFrequency = 0;
            for (int i = 0; i < listOrderedImageCountList.Count; i++)
            {
                nTotFrequency += listOrderedImageCountList[i].ClassCount;
                double nPerFrequency = Math.Round((nTotFrequency * 1.0 / nTotal * 1.0) * 100, 2);
                listOrderedImageCountList[i].FrequencyPercent = nPerFrequency;
            }

            DistByImageCount = null;
            DistByImageCount = listOrderedImageCountList.Any(item => item.ClassCount > 0)? listOrderedImageCountList : null;
        }


        private object _distByImageCount;
        public object DistByImageCount
        {
            get
            {
                return _distByImageCount;
            }
            set
            {
                if (_distByImageCount != value)
                {
                    _distByImageCount = value;
                    NotifyPropertyChanged("DistByImageCount");
                }
            }
        }

        private void LoadClasswiseLabelCountData()
        {
            List<ClassWiseCount> listClassWiseLabelCount = new List<ClassWiseCount>();
            Dictionary<string, string> tempDictList = new Dictionary<string, string>();
            //foreach (var curItem in settings.dictEVSupervisorClass)
            //{
            //    string className = curItem.Value.Split('(', ')').Length > 0 ? curItem.Value.Split('(', ')')[0] : "Unknown Class";
            //    string classAlias = curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0];
            //    tempDictList.Add(classAlias, className);
            //}

            //foreach (ClassFolderStat curClassFolder in ListClassFolderStat)
            //{
            //    string classAlias = curClassFolder.ClassAliasName;
            //    string className = tempDictList.ContainsKey(classAlias) ? tempDictList[classAlias] : "Unknown Class";

            //    ClassWiseCount curClassWiseCount = listClassWiseLabelCount.FirstOrDefault(item => item.ClassAlias.ToUpper() == classAlias.ToUpper());
            //    if (curClassWiseCount == null)
            //    {
            //        listClassWiseLabelCount.Add(new ClassWiseCount()
            //        {
            //            ClassAlias = classAlias,
            //            ClassName = className,
            //            ClassCount = curClassFolder.ClassCount
            //        });
            //    }
            //    else
            //    {
            //        curClassWiseCount.ClassCount += curClassFolder.ClassCount;
            //    }               
            //}

            foreach (var curItem in settings.dictEVSupervisorClass)
            {
                string className = curItem.Value.Split('(', ')').Length > 0 ? curItem.Value.Split('(', ')')[0] : "Unknown Class";
                string classAlias = curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0];
                int count = ImageMenuList.SelectMany(item => item.ImageBox.ListImageClass.Where(s => s.ClassAlias != null && s.ClassAlias.ToUpper() == classAlias.ToUpper())).Count();
                
                ClassWiseCount curClassWiseCount = listClassWiseLabelCount.FirstOrDefault(item => item.ClassAlias.ToUpper() == classAlias.ToUpper());
                if (curClassWiseCount == null)
                {
                    listClassWiseLabelCount.Add(new ClassWiseCount()
                    {
                        ClassAlias = classAlias,
                        ClassName = className,
                        ClassCount = count
                    });
                }
                else
                {
                    curClassWiseCount.ClassCount += count;
                }
            }
            var listOrderedClasswiseLabel = listClassWiseLabelCount.OrderByDescending(item => item.ClassCount).ToList();
            int nTotal = listOrderedClasswiseLabel.Sum(item => item.ClassCount);
            int nTotFrequency = 0;
            for (int i = 0; i < listOrderedClasswiseLabel.Count; i++)
            {
                nTotFrequency += listOrderedClasswiseLabel[i].ClassCount;
                double nPerFrequency = Math.Round((nTotFrequency * 1.0 / nTotal * 1.0) * 100, 2);
                listOrderedClasswiseLabel[i].FrequencyPercent = nPerFrequency;
            }

            DistByLabelCount = null;
            DistByLabelCount = listOrderedClasswiseLabel.Any(item => item.ClassCount > 0) ? listOrderedClasswiseLabel : null; ;
        }

        private object _distByLabelCount;
        public object DistByLabelCount
        {
            get
            {
                return _distByLabelCount;
            }
            set
            {
                if (_distByLabelCount != value)
                {
                    _distByLabelCount = value;
                    NotifyPropertyChanged("DistByLabelCount");
                }
            }
        }

        public void LoadLabellingStatisticsData()
        {
            List<ClassWiseCount> listLabelStatsCount = new List<ClassWiseCount>();

            listLabelStatsCount.Add(new ClassWiseCount()
            {
                ClassName = settings.ClassType == EnumClassType.Segregation? "Unsegregated Images" : "Unlabelled Images",
                ClassCount = TotalUnlabelledImages
            });
            listLabelStatsCount.Add(new ClassWiseCount()
            {
                ClassName = settings.ClassType == EnumClassType.Segregation ? "Segregated Images" : "Labelled Images",
                ClassCount = TotalLabelledImages
            });
            listLabelStatsCount.Add(new ClassWiseCount()
            {
                ClassName = "Correction Images",
                ClassCount = TotalCorrectionImages
            });

            listLabelStatsCount.RemoveAll(item => item.ClassCount == 0);
            DistByLabelStat = null;
            DistByLabelStat = listLabelStatsCount;
        }

        private object _distByLabelStat;
        public object DistByLabelStat
        {
            get
            {
                return _distByLabelStat;
            }
            set
            {
                if (_distByLabelStat != value)
                {
                    _distByLabelStat = value;
                    NotifyPropertyChanged("DistByLabelStat");
                }
            }
        }

        public void LoadClasswiseTimeAnalysisGraph()
        {
            try
            {
                ListClassWiseTimeAnalyses = new List<ClassWiseTimeAnalysis>();
                Dictionary<string, string> tempDictList = new Dictionary<string, string>();
                foreach (var curItem in settings.dictEVSupervisorClass)
                {
                    string className = curItem.Value.Split('(', ')').Length > 0 ? curItem.Value.Split('(', ')')[0] : "Unknown Class";
                    string classAlias = curItem.Value.Split('(', ')').Length > 1 ? curItem.Value.Split('(', ')')[1] : curItem.Value.Split('(', ')')[0];
                    tempDictList.Add(classAlias, className);
                }

                foreach (ClassFolderStat curFolderStat in ListClassFolderStat)
                {
                    string strDateDetail = File.ReadLines(curFolderStat.ImportDatasheetName).FirstOrDefault();
                    if (strDateDetail != null)
                        strDateDetail = strDateDetail.Split(',').Length > 0 ? strDateDetail.Split(',')[0] : "";

                    string strDate = strDateDetail.Replace("Date : ", "");
                    if (strDate == string.Empty)
                        continue;

                    DateTime dtDatasheetDate = DateTime.ParseExact(strDate, "MM-dd-yyyy HH:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                    string classAlias = curFolderStat.ClassAliasName;
                    string className = tempDictList.ContainsKey(classAlias) ? tempDictList[classAlias] : "Unknown Class";

                    ClassWiseTimeAnalysis curClassTimeAnalysis = ListClassWiseTimeAnalyses.FirstOrDefault(item => item.Alias.ToUpper() == classAlias.ToUpper());
                    if (curClassTimeAnalysis == null)
                    {
                        curClassTimeAnalysis = new ClassWiseTimeAnalysis();
                        curClassTimeAnalysis.ClassName = className;
                        curClassTimeAnalysis.Alias = classAlias;

                        WeeklyAnalysis curWeeklyAnalysis = new WeeklyAnalysis();
                        curWeeklyAnalysis.DatasheetDate = dtDatasheetDate;
                        curWeeklyAnalysis.WeekName = "Week1";
                        curWeeklyAnalysis.ClassCount = curFolderStat.ClassCount;
                        curClassTimeAnalysis.listWeeklyAnalysis.Add(curWeeklyAnalysis);
                        ListClassWiseTimeAnalyses.Add(curClassTimeAnalysis);
                    }
                    else
                    {
                        List<WeeklyAnalysis> tempList = new List<WeeklyAnalysis>();
                        bool bIsSameWeek = false;
                        for (int i = 0; i < curClassTimeAnalysis.listWeeklyAnalysis.Count; i++)
                        {
                            if (IsDateInSameWeek(curClassTimeAnalysis.listWeeklyAnalysis[i].DatasheetDate, dtDatasheetDate))
                            {
                                curClassTimeAnalysis.listWeeklyAnalysis[i].ClassCount += curFolderStat.ClassCount;
                                bIsSameWeek = true;
                            }
                        }

                        if (!bIsSameWeek)
                        {
                            curClassTimeAnalysis.listWeeklyAnalysis.Add(new WeeklyAnalysis()
                            {
                                WeekName = "Week" + (curClassTimeAnalysis.listWeeklyAnalysis.Count + 1),
                                DatasheetDate = dtDatasheetDate,
                                ClassCount = curFolderStat.ClassCount
                            });
                        }
                    }
                    int total = curClassTimeAnalysis.listWeeklyAnalysis.Sum(i => i.ClassCount);
                    curClassTimeAnalysis.listWeeklyAnalysis.ForEach(item => item.TotalCount = total);
                }

                foreach (ClassWiseTimeAnalysis curAnalysis in ListClassWiseTimeAnalyses)
                {
                    if (curAnalysis.listWeeklyAnalysis.Count == 1)
                        curAnalysis.dictPercentageChange.Add("Week1", "-");
                    else if (curAnalysis.listWeeklyAnalysis.Count > 1)
                    {
                        for (int i = 1; i < curAnalysis.listWeeklyAnalysis.Count; i++)
                        {
                            int total = curAnalysis.listWeeklyAnalysis[i - 1].ClassCount + curAnalysis.listWeeklyAnalysis[i].ClassCount;
                            double firstVal = (curAnalysis.listWeeklyAnalysis[i - 1].ClassCount * 1.0 / total * 1.0) * 100;
                            double secVal = (curAnalysis.listWeeklyAnalysis[i].ClassCount * 1.0 / total * 1.0) * 100;
                            double perChange = Math.Round(secVal - firstVal, 2);
                            string key = curAnalysis.listWeeklyAnalysis[i - 1].WeekName + " - " + curAnalysis.listWeeklyAnalysis[i].WeekName;
                            curAnalysis.dictPercentageChange.Add(key, perChange.ToString() + "%");
                        }
                    }
                }
                List<string> listClassDefects = ListClassWiseTimeAnalyses.Select(item => item.ClassName).ToList();
                pageTimeAnalysisGrpaph.cmbDefectFilter.ItemsSource = listClassDefects;
                pageTimeAnalysisGrpaph.cmbDefectFilter.SelectedIndex = 0;
                Utilities.LogMessage("Data Insights graphs loaded successfully.");
            }
            catch (Exception ex)
            {
                Utilities.LogMessage("DataVisualizationInterface::LoadClasswiseTimeAnalysisGraph: " + ex.Message, 9);
            }
        }

        private bool IsDateInSameWeek(DateTime firstDate, DateTime sencondDate)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;

            int date1Week = calendar.GetWeekOfYear(firstDate, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
            int date2Week = calendar.GetWeekOfYear(sencondDate, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

            return date1Week == date2Week ? true : false;
        }

        private object _distByTimeWiseClass;
        public object DistByTimeWiseClass
        {
            get
            {
                return _distByTimeWiseClass;
            }
            set
            {
                if (_distByTimeWiseClass != value)
                {
                    _distByTimeWiseClass = value;
                    NotifyPropertyChanged("DistByTimeWiseClass");
                }
            }
        }

        private void borderView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Button).Name == "borderPreView")
                CurIndex--;
            else if ((sender as Button).Name == "borderNextView")
                CurIndex++;

            if (CurIndex < 0 || CurIndex >= ListDataInsightView.Count)
                return;

            if(settings.ClassType == EnumClassType.Segregation)
            {
                lblDataInsightHeading.Content = CurIndex == 0 ? "Classwise Graph - Segregation Image Count" : CurIndex == 1 ? "Segregation Statistics Graph" : CurIndex == 2 ? "Time Analysis Graph" : "Classwise Graph - Segregation Image Count";
            }
            else
            {
                lblDataInsightHeading.Content = CurIndex == 0 ? "Classwise Graph - Image Count" : CurIndex == 1 ? "Classwise Graph - Label Count" : CurIndex == 2 ? "Label Statistics Graph" : CurIndex == 3 ? "Time Analysis Graph" : "Classwise Graph - Image Count";
            }


            gridDataInsightView.Children.Clear();
            gridDataInsightView.Children.Add(ListDataInsightView[CurIndex]);
            if (CurIndex == ListDataInsightView.Count - 1)
                borderNextView.Visibility = Visibility.Collapsed;
            else if (CurIndex < ListDataInsightView.Count - 1)
                borderNextView.Visibility = Visibility.Visible;

            if (CurIndex == 0)
                borderPreView.Visibility = Visibility.Collapsed;
            else if (CurIndex > 0)
                borderPreView.Visibility = Visibility.Visible;
        }
    }

    public class ClassWiseCount
    {
        public string ClassName { get; set; }
        public string ClassAlias { get; set; }
        public int ClassCount { get; set; }
        public double FrequencyPercent { get; set; }
    }

    public class ClassWiseTimeAnalysis
    {
        public string ClassName { get; set; }
        public string Alias { get; set; }
        public List<WeeklyAnalysis> listWeeklyAnalysis { get; set; }
        public Dictionary<string, string> dictPercentageChange { get; set; }

        public ClassWiseTimeAnalysis()
        {
            listWeeklyAnalysis = new List<WeeklyAnalysis>();
            dictPercentageChange = new Dictionary<string, string>();
        }
    }

    public class WeeklyAnalysis
    {
        public DateTime DatasheetDate { get; set; }
        public string WeekName { get; set; }
        public int ClassCount { get; set; }
        public int TotalCount { get; set; }

        private double rate = 0.0;
        public double RateChange
        {
            get
            {
                return Math.Round((ClassCount * 1.0 / TotalCount * 1.0) * 100, 2);
            }
            set
            {
                rate = value;
            }
        }
    }
}
