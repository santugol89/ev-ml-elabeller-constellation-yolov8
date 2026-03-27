using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for ImageStatsWindow.xaml
    /// Puneeth H.S
    /// </summary>
    /// 
    public partial class ImageStatsWindow : Window
    {

        MainWindow app;
        public BackgroundWorker bgWorker;

        public ImageStatsWindow(MainWindow app)
        {

            InitializeComponent();
            this.app = app;

            double currentImageHeight;
            double currentImageWidth;
            ObservableCollection<ImageMenu> imageMenuList1 = app.ImageMenuList;

            foreach (var item in imageMenuList1)
                using (FileStream stream = Delimon.Win32.IO.File.OpenRead(item.ImagePath))
                {
                    try
                    {
                        BitmapImage bmpImage = new BitmapImage();
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = stream;
                        bmpImage.EndInit();
                        currentImageHeight = bmpImage.PixelHeight;
                        currentImageWidth = bmpImage.PixelWidth;
                        TextBlock.Text = " Resolution: " + currentImageWidth + " * " + currentImageHeight;
                        break;
                    }
                    catch { }
                }


            foreach (var item in imageMenuList1)
            {
                TreeView childItem1 = new TreeView() { Title =item.ImageSlNo+".  "+ item.ImageName };

                if (item.ImageBox.ListImageClass.Count > 0)
                {
                    foreach (var item1 in item.ImageBox.ListImageClass)
                    {
                        childItem1.Items.Add(new TreeView() { Title = "[" + item1.ClassName + ": " + item1.Score + "]" });
                        childItem1.ItemsColors =Brushes.LightGreen;
                    }
                    trvMenu.Items.Add(childItem1);
                }
                else
                {
                    trvMenu.Items.Add(childItem1);
                }
            }
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
        public class TreeView : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            public TreeView()
            {
                this.Items = new ObservableCollection<TreeView>();
                this.ItemsColors = Brushes.WhiteSmoke;
            }
            public Brush ItemsColor = Brushes.WhiteSmoke;
            public Brush ItemsColors
            {
                get
                {
                    return ItemsColor;
                }
                set
                {
                    ItemsColor = value;
                    NotifyPropertyChanged("ItemsColors");

                }
            }
            public string Title { get; set; }
            public ObservableCollection<TreeView> Items { get; set; }
        }
        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.Topmost = false;
            this.Activate();
            trvMenu.Focus();
        }

    }
}