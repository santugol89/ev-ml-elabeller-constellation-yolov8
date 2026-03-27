using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace GenieSupervisor
{
    public enum HitType
    {
        None, Body, UL, UR, LR, LL, T, B, L, R, Edge
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnumSelectedShape
    {
        [EnumMember(Value = "rect")]
        Rectangle,
        [EnumMember(Value = "circ")]
        Circle,
        [EnumMember(Value = "polyline")]
        Polyline,
        Null
    }

    public enum EnumModeData
    {
        GroundTruthData, PredictedData
    }

    public enum EnumClassType
    {
        Rectangle, Circle, Polyline, Any, Segregation, None
    }
    public enum EnumAugmentionType
    {
        FlipH, FlipV, Noise, Rotate, Trans, Blur, None
    }

    public class ImageMenu: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [JsonIgnore]
        public string ImagePath { get; set;}

        [JsonProperty(Order = 0, PropertyName = "")]
        public string ImageName { get; set;}

        [JsonIgnore]
        public Brush menuItemBrush = Brushes.White;
        public Brush MenuItemBrush
        {
            get
            {
                return menuItemBrush;
            }
            set
            {
                menuItemBrush = value;
                NotifyPropertyChanged("MenuItemBrush");
            }
        }

        public ImageListBox ImageBox { get; set; }

        public string ImageSlNo { get; set; }

        public ImageMenu(string curImageName)
        {
            ImageBox = new ImageListBox(curImageName);
        }
    }

    public class ImageClass
    {
        public string ClassIndex { get; set; }

        public string ClassName { get; set; }

        public string ClassAlias { get; set; }

        public double XCoordinate { get; set; }

        public double YCoordinate { get; set; }

        public List<double> All_Points_X { get; set; }

        public List<double> All_Points_Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string ShapeCoordinates { get; set; }

        public bool Reviewed { get; set; }

        public EnumSelectedShape Shape { get; set; }

        public EnumModeData DataTypeMode { get; set; }

        public string Score { get; set; }

        private Brush _highlightStroke = Brushes.Red;
        public Brush HighLightStroke
        {
            get
            {
                if (DataTypeMode == EnumModeData.PredictedData)
                    return _highlightStroke = Brushes.DarkViolet;
                else
                    return _highlightStroke = Brushes.Red;
            }

            set
            {
                _highlightStroke = value;
            }
        }

        private Brush _selectionStroke = Brushes.Blue;
        public Brush SelectionStroke
        {
            get
            {
                if (DataTypeMode == EnumModeData.PredictedData)
                    return _selectionStroke = Brushes.LightYellow;
                else
                    return _selectionStroke = Brushes.Blue;
            }

            set
            {
                _selectionStroke = value;
            }
        }

        public string ImportDatasheetName { get; set; }

        public int Rotation { get; set; }
        public Shape DrawShape { get; set; }
        public Label DrawLabel { get; set; }

        public ImageClass(string _classIndex, string _className)
        {
            ClassIndex = _classIndex;
            ClassName = _className;
            All_Points_X = new List<double>();
            All_Points_Y = new List<double>();
            ShapeCoordinates = "";
        }
    }

    public class ImageListBox:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        ObservableCollection<ImageClass> listImageClass = new ObservableCollection<ImageClass>();
        public ObservableCollection<ImageClass> ListImageClass
        {
            get
            {

                return listImageClass;
            }
            set
            {
                listImageClass = value;
                NotifyPropertyChanged("ListImageClass");
            }
        }

        public string ImageBoxName { get; set;}

        public int Imagewidth { get; set; }

        public int ImageHeight { get; set; }

        public ImageListBox(string curImageName)
        {
            this.ImageBoxName = curImageName;
            ListImageClass = new ObservableCollection<ImageClass>();
        }
    }
}
