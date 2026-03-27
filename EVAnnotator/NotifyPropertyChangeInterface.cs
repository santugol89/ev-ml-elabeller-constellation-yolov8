using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GenieSupervisor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        /// <summary>
        /// Function to Display the ROI dimension in status bar 
        /// </summary>
        private string statusSelectedDimension = "";
        public string StatusSelectedDimension
        {
            get
            {
                return statusSelectedDimension;
            }
            set
            {
                statusSelectedDimension = value;
                NotifyPropertyChanged("StatusSelectedDimension");
            }
        }

        public string LoggedAsLabel
        {
            get
            {
                return "Logged as " + UserName;
            }
            set
            {
                NotifyPropertyChanged("LoggedAsLabel");
            }
        }

        public string SelectedProject
        {
            get
            {
                return "Project : " + settings.dictProjectList[settings.CurrentProject] + " [" + settings.Architecture + "]";
            }
            set
            {
                NotifyPropertyChanged("SelectedProject");
            }
        }

        public string LabelStatHeader
        {
            get
            {
                return settings.ClassType == EnumClassType.Segregation? "Segregation Stats": "Labelling Stats";
            }
            set
            {
                NotifyPropertyChanged("LabelStatHeader");
            }
        }

        public string UnLabelledImagesContent
        {
            get
            {
                return settings.ClassType == EnumClassType.Segregation ? "Unsegregated Images" : "Unlabelled Images";
            }
            set
            {
                NotifyPropertyChanged("UnLabelledImagesContent");
            }
        }

        public string LabelledImagesContent
        {
            get
            {
                return settings.ClassType == EnumClassType.Segregation ? "Segregated Images" : "Labelled Images";
            }
            set
            {
                NotifyPropertyChanged("LabelledImagesContent");
            }
        }

        public double AttributeListViewWidth
        {
            get
            {
                if (settings.ClassType == EnumClassType.Segregation)
                    return 0;
                else
                {
                    GridView g = lvImageClass.View as GridView;
                    double total = 0;
                    for (int i = 0; i < g.Columns.Count - 1; i++)
                    {
                        total += g.Columns[i].Width;
                    }
                    return (lvImageClass.ActualWidth - total);
                }
            }
            set
            {
                NotifyPropertyChanged("AttributeListViewWidth");
            }
        }

        public double ClassNameListViewWidth
        {
            get
            {
                if (settings.ClassType == EnumClassType.Segregation)
                {
                    GridView g = lvImageClass.View as GridView;
                    double total = 0;
                    for (int i = 0; i < g.Columns.Count - 2; i++)
                    {
                        total += g.Columns[i].Width;
                    }
                    return (lvImageClass.ActualWidth - total);
                }
                else
                {
                    return 300;
                }
            }
            set
            {
                NotifyPropertyChanged("ClassNameListViewWidth");
            }
        }

        /// <summary>
        /// Function to visible/collapse status note in status bar 
        /// </summary>
        private Visibility _statusNoteVisiblity = Visibility.Collapsed;
        public Visibility StatusNoteVisiblity
        {
            get
            {
                return _statusNoteVisiblity;
            }
            set
            {
                _statusNoteVisiblity = value;
                NotifyPropertyChanged("StatusNoteVisiblity");
            }
        }

        /// <summary>
        /// Function to enable or disable class name combo box selection 
        /// </summary>
        private bool isEnableClassSP = false;
        public bool IsEnableClassStackPanel
        {
            get
            {
                return isEnableClassSP;
            }
            set
            {
                isEnableClassSP = value;
                NotifyPropertyChanged("IsEnableClassStackPanel");
            }
        }

        /// <summary>
        /// Function to enable/disable Rectangle shape in tool palette 
        /// </summary>
        private bool isEnableRectangle = true;
        public bool IsEnableRectangle
        {
            get { return isEnableRectangle; }
            set
            {
                isEnableRectangle = value;
                NotifyPropertyChanged("IsEnableRectangle");
            }
        }

        /// <summary>
        /// Function to enable/disable circle shape in tool palette 
        /// </summary>
        private bool isEnableCircle = false;
        public bool IsEnableCircle
        {
            get { return isEnableCircle; }
            set
            {
                isEnableCircle = value;
                NotifyPropertyChanged("IsEnableCircle");
            }
        }

        /// <summary>
        /// Function to enable/disable Polygon shape in tool palette 
        /// </summary>
        private bool isEnablePoly = false;
        public bool IsEnablePoly
        {
            get { return isEnablePoly; }
            set
            {
                isEnablePoly = value;
                NotifyPropertyChanged("IsEnablePoly");
            }
        }

        /// <summary>
        /// Function to Visible/Collapse Shape Quick Pallete 
        /// </summary>
        private Visibility isVisibleShapeQuickPallete = Visibility.Collapsed;
        public Visibility IsVisibleShapeQuickPallete
        {
            get { return isVisibleShapeQuickPallete; }
            set
            {
                isVisibleShapeQuickPallete = value;
                NotifyPropertyChanged("IsVisibleShapeQuickPallete");
            }
        }

        private Visibility isVisibleInWorkshellMode = Visibility.Visible;
        public Visibility IsVisibleInWorkshellMode
        {
            get { return isVisibleInWorkshellMode; }
            set
            {
                isVisibleInWorkshellMode = value;
                NotifyPropertyChanged("IsVisibleInWorkshellMode");
            }
        }

        /// <summary>
        /// Function to Visible/Collapse Export CSV Button 
        /// </summary>
        private Visibility isVisibleMultiCSVExport = Visibility.Collapsed;
        public Visibility IsVisibleMultiCSVExport
        {
            get
            {
                if (IsVisibleInWorkshellMode == Visibility.Collapsed)
                    isVisibleMultiCSVExport = Visibility.Collapsed;
                return isVisibleMultiCSVExport;
            }
            set
            {
                isVisibleMultiCSVExport = value;
                NotifyPropertyChanged("IsVisibleMultiCSVExport");
            }
        }

        /// <summary>
        /// Function to set the thickness of stroke of shape drawn 
        /// </summary>
        private int strokeThickness = 2;
        public int ShapeStrokeThickness
        {
            get { return strokeThickness; }
            set { strokeThickness = value; }
        }


        /// <summary>
        /// Function to Display the image name selected in status bar 
        /// </summary>
        private string statusBarImageFile = "";
        public string StatusBarImageFile
        {
            get
            {
                return statusBarImageFile;
            }
            set
            {
                statusBarImageFile = value;
                NotifyPropertyChanged("StatusBarImageFile");
            }
        }

        /// <summary>
        /// Function to Display the Image dimension in status bar 
        /// </summary>
        private string statusBarDimension = "";
        public string StatusBarDimension
        {
            get
            {
                return statusBarDimension;
            }
            set
            {
                statusBarDimension = value;
                NotifyPropertyChanged("StatusBarDimension");
            }
        }

    }
}
