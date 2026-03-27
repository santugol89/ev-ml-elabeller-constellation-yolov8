using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenieSupervisor
{
    public class JsonShapeAttributes
    {
        [DefaultValue(0.0), JsonProperty(Order = 2, PropertyName = "x")]
        public double XCoordinate { get; set; }

        [DefaultValue(0.0), JsonProperty(Order = 3, PropertyName = "y")]
        public double YCoordinate { get; set; }

        [JsonProperty(Order = 2, PropertyName = "all_points_x")]
        public List<double> All_Points_X { get; set; }

        [JsonProperty(Order = 3, PropertyName = "all_points_y")]
        public List<double> All_Points_Y { get; set; }

        [DefaultValue(0.0), JsonProperty(Order = 4, PropertyName = "width")]
        public double Width { get; set; }

        [DefaultValue(0.0), JsonProperty(Order = 5, PropertyName = "height")]
        public double Height { get; set; }

        [JsonProperty(Order = 1, PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Include)]
        public EnumSelectedShape Shape { get; set; }

    }

    public class JsonClassAttributes
    {
        [JsonProperty(Order = 1, PropertyName = "class id")]
        public string ClassIndex { get; set; }

        [JsonProperty(Order = 2, PropertyName = "class name")]
        public string ClassName { get; set; }

        [JsonProperty(Order = 3, PropertyName = "review")]
        public string Review { get; set; }
    }
}
