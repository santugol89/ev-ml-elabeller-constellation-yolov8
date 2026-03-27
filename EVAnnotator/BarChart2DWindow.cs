using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Telerik.Windows.Controls.ChartView;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private void UpdateAutoLabellerClass2DGraph(string CurrentProject)
        {
            List<BarSeries> listNonStackBarSeries = new List<BarSeries>();
            List<BarSeries> listStackedBarSeries = new List<BarSeries>();

            for (int track = 0; track < ListClassFolderStat.Count; track++)
            {
                if (ListClassFolderStat[track].ClassCount <= 0)
                    continue;

                var converter = new System.Windows.Media.BrushConverter();

                FrameworkElementFactory spFactoryParent = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                spFactoryParent.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Vertical);
                spFactoryParent.SetValue(System.Windows.Controls.StackPanel.BackgroundProperty, (Brush)converter.ConvertFromString("#FF383838"));
                spFactoryParent.SetValue(System.Windows.Controls.StackPanel.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

                FrameworkElementFactory spTextBlockParent = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.BackgroundProperty, (Brush)converter.ConvertFromString("#FF383838"));
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

                FrameworkElementFactory tbToolTipInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, string.Format(ListClassFolderStat[track].ClassFolderName));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, Brushes.LightGoldenrodYellow);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Left);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
                spTextBlockParent.AppendChild(tbToolTipInnfo);

                tbToolTipInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, string.Format("  " + ListClassFolderStat[track].ClassCount.ToString()));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, (Brush)converter.ConvertFromString("#FF19D32F"));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Right);
                tbToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Right);
                spTextBlockParent.AppendChild(tbToolTipInnfo);

                spFactoryParent.AppendChild(spTextBlockParent);
                var plotNonStackInfo = new TrackPlotInfo();
                plotNonStackInfo.XCategory = ListClassFolderStat[track].ClassAliasName;
                plotNonStackInfo.YAllClassValue = ListClassFolderStat[track].ClassCount;

                BarSeries NonStackBarSeries = new BarSeries();
                DataTemplate template = new DataTemplate();
                template.VisualTree = spFactoryParent;
                NonStackBarSeries.TooltipTemplate = template;

                NonStackBarSeries.DefaultVisualStyle = NonStackedColor2D();
                NonStackBarSeries.DataPoints.Add(new Telerik.Charting.CategoricalDataPoint
                {
                    Category = plotNonStackInfo.XCategory,
                    Value = plotNonStackInfo.YAllClassValue
                });
                listNonStackBarSeries.Add(NonStackBarSeries);

                var plotStackBarInfo = new TrackPlotInfo() { StackedYValues = new List<double>()};
                plotStackBarInfo.StackedColorBrush = new List<Brush>();
                plotStackBarInfo.XCategory = ListClassFolderStat[track].ClassAliasName; 
                double[] ImageTypeCount = new double[] { ListClassFolderStat[track].SingleSpotCount, ListClassFolderStat[track].PhaseContrastCount };

                FrameworkElementFactory spStackedFactoryParent = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                spStackedFactoryParent.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Vertical);
                spStackedFactoryParent.SetValue(System.Windows.Controls.StackPanel.BackgroundProperty, (Brush)converter.ConvertFromString("#FF383838"));
                spStackedFactoryParent.SetValue(System.Windows.Controls.StackPanel.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

                spTextBlockParent = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.BackgroundProperty, (Brush)converter.ConvertFromString("#FF383838"));
                spTextBlockParent.SetValue(System.Windows.Controls.StackPanel.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

                FrameworkElementFactory tbStackedToolTipInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, string.Format(ListClassFolderStat[track].ClassFolderName));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, Brushes.LightGoldenrodYellow);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Left);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
                spTextBlockParent.AppendChild(tbStackedToolTipInnfo);

                tbStackedToolTipInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, string.Format("  " + ListClassFolderStat[track].ClassCount.ToString()));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, (Brush)converter.ConvertFromString("#FF19D32F"));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Right);
                tbStackedToolTipInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Right);

                spTextBlockParent.AppendChild(tbStackedToolTipInnfo);
                spStackedFactoryParent.AppendChild(spTextBlockParent);

                for (int i = 0; i < ImageTypeCount.Length; i++)
                {
                    if (ImageTypeCount[i] == 0)
                        continue;

                    plotStackBarInfo.StackedYValues.Insert(0, ImageTypeCount[i]);
                    plotStackBarInfo.StackedColorBrush.Insert(0, StatckedGraphBrushes[i]);

                    FrameworkElementFactory spStackTextBlock= new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                    spStackTextBlock.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                    spStackTextBlock.SetValue(System.Windows.Controls.StackPanel.BackgroundProperty, (Brush)converter.ConvertFromString("#FF383838"));
                    spStackTextBlock.SetValue(System.Windows.Controls.StackPanel.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

                    FrameworkElementFactory tbToolTipStackInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, i == 0? "Single Spot : " : i == 1? "Phase Contrast : " : "");
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, StatckedGraphBrushes[i]);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Left);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
                    spStackTextBlock.AppendChild(tbToolTipStackInnfo);

                    tbToolTipStackInnfo = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextProperty, string.Format(ImageTypeCount[i].ToString()));
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 15d);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, StatckedGraphBrushes[i]);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, Brushes.Transparent);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(2));
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.TextAlignmentProperty, TextAlignment.Right);
                    tbToolTipStackInnfo.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Right);
                    spStackTextBlock.AppendChild(tbToolTipStackInnfo);
                    spStackedFactoryParent.AppendChild(spStackTextBlock);
                }

                BarSeries StackBarSeries = new BarSeries();
                template = new DataTemplate();
                template.VisualTree = spStackedFactoryParent;
                StackBarSeries.TooltipTemplate = template;

                StackBarSeries.DefaultVisualStyle = SelectStackedColor2D(plotStackBarInfo);
                StackBarSeries.DataPoints.Add(new Telerik.Charting.CategoricalDataPoint
                {
                    Category = plotStackBarInfo.XCategory,
                    Value = plotStackBarInfo.YValue
                });
                listStackedBarSeries.Add(StackBarSeries);
            }

            ChartNonStackClass.Series.Clear();
            for (int count = 0; count < listNonStackBarSeries.Count; count++)
                ChartNonStackClass.Series.Add(listNonStackBarSeries[count]);

            ChartStackedClass.Series.Clear();
            for (int count = 0; count < listStackedBarSeries.Count; count++)
                ChartStackedClass.Series.Add(listStackedBarSeries[count]);

            lvAutoPilotBVStat.ItemsSource = ListClassFolderStat;
            lvAutoPilotBVStat.Items.Refresh();
            lvAutoPilotIPIeStat.ItemsSource = ListClassFolderStat;
            lvAutoPilotIPIeStat.Items.Refresh();
            lblAutoPilotStatus.Content = "Last Auto Pilot : " + dtLastAutoPilotTime.ToShortDateString() + " " + dtLastAutoPilotTime.ToShortTimeString();

            if (settings.dictProjectList[CurrentProject].Contains("BV") && AutoPilotTotalImages != AutoPilotNonProcImages)
            {
                IsVisibleAutoLabellerBVStats = Visibility.Visible;
                IsVisibleAutoLabellerIPIeStats = Visibility.Collapsed;
                btnStackChart.Visibility = Visibility.Visible;
            }
            else if (AutoPilotTotalImages != AutoPilotNonProcImages)
            {
                IsVisibleAutoLabellerBVStats = Visibility.Collapsed;
                IsVisibleAutoLabellerIPIeStats = Visibility.Visible;
                btnStackChart.Visibility = Visibility.Collapsed;
            }
            btnBarChart.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowLeft;
            btnBarChart_Click(null, null);
        }

        public Style NonStackedColor2D()
        {
            var brush = new LinearGradientBrush() { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            var converter = new System.Windows.Media.BrushConverter();

            Style style = new Style(typeof(Border));
            style.Setters.Add(new Setter(BackgroundProperty, (Brush)converter.ConvertFromString("#FF19D32F")));
            style.Setters.Add(new Setter(OpacityProperty, 0.7));
            //style.Setters.Add(new Setter(op));
            return style;
        }

        public Style SelectStackedColor2D(TrackPlotInfo dataItem)
        {
            var brush = new LinearGradientBrush() { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            var yValue = dataItem.YValue;
            List<int> myKeys = new List<int>();

            if (yValue != 0)
            {
                double plusOffset = 0.01 / yValue;

                double currentOffset = 0;
                for (int i = dataItem.StackedYValues.Count - 1; i >= 0; i--)
                {
                    var paletteBrush = (SolidColorBrush)dataItem.StackedColorBrush[i];
                    if (brush.GradientStops.Count > 0)
                    {
                        brush.GradientStops.Add(new GradientStop(Brushes.Black.Color, brush.GradientStops[brush.GradientStops.Count - 1].Offset));
                        brush.GradientStops.Add(new GradientStop(Brushes.Black.Color, currentOffset + plusOffset));
                    }
                    currentOffset += dataItem.StackedYValues[i] / yValue;

                    if (brush.GradientStops.Count > 0)
                    {
                        brush.GradientStops.Add(new GradientStop(paletteBrush.Color, brush.GradientStops[brush.GradientStops.Count - 1].Offset));
                    }

                    var stop = new GradientStop(paletteBrush.Color, currentOffset);
                    brush.GradientStops.Add(stop);
                }
            }

            Style style = new Style(typeof(Border));
            style.Setters.Add(new Setter(BackgroundProperty, brush));
            style.Setters.Add(new Setter(OpacityProperty, 0.7));
            //style.Setters.Add(new Setter(op));
            return style;
        }
    }

    public class TrackPlotInfo
    {
        public string XCategory { get; set; }

        public double YAllClassValue { get; set; }

        public List<double> StackedYValues { get; set; }
        public List<Brush> StackedColorBrush { get; set; }

        public double YValue
        {
            get { return this.StackedYValues.Sum(); }
        }
    }
}
