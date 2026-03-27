using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GenieSupervisor.Data_Insights
{
    /// <summary>
    /// Interaction logic for UControlTimeAnalysisGraph.xaml
    /// </summary>
    public partial class UControlTimeAnalysisGraph : UserControl
    {
        public MainWindow app; 
        public UControlTimeAnalysisGraph()
        {
            InitializeComponent();
            //DataContext = this;
        }

        private void cmbDefectFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDefectFilter.SelectedItem == null)
                return;

            app.DistByTimeWiseClass = null;
            var tempTimeAnalysis = app.ListClassWiseTimeAnalyses.FirstOrDefault(item => item.ClassName.ToUpper() == cmbDefectFilter.SelectedItem.ToString().ToUpper());
            if (tempTimeAnalysis == null)
                return;
            app.DistByTimeWiseClass = tempTimeAnalysis.listWeeklyAnalysis;
            lvChangeofPercent.ItemsSource = tempTimeAnalysis.dictPercentageChange;
        }
    }
}
