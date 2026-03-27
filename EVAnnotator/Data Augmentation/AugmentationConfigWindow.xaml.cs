using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenieSupervisor.Data_Augmentation
{
    /// <summary>
    /// Interaction logic for AugmentationConfigWindow.xaml
    /// </summary>
    public partial class AugmentationConfigWindow : Telerik.Windows.Controls.RadWindow
    {
        MainWindow app;
        EnumAugmentionType AugType;
        public AugmentationConfigWindow(MainWindow app, EnumAugmentionType Type)
        {
            InitializeComponent();
            this.app = app;
            AugType = Type;
            InitializeControls();
            DataContext = this;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void InitializeControls()
        {
            txtNoiseValue.Text = app.settings.CurrentAugmentConfig.NoiseValue.ToString();
            //if (app.settings.CurrentAugmentConfig.RotateDegree == 90)
            //    radRot_90.IsChecked = true;
            //else if(app.settings.CurrentAugmentConfig.RotateDegree == 45)
            //    radRot_45.IsChecked = true;
            txtAngle.Text = app.settings.CurrentAugmentConfig.RotateDegree.ToString();
            txtXAxis.Text = app.settings.CurrentAugmentConfig.Trans_Coordinate[0].ToString();
            txtYAxis.Text = app.settings.CurrentAugmentConfig.Trans_Coordinate[1].ToString();
            txtBlurRatio.Text = app.settings.CurrentAugmentConfig.BlurRatio.ToString();
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private bool IsValidData()
        {
            if (txtNoiseValue.Text.Contains("..") || txtXAxis.Text.Contains("..") || txtYAxis.Text.Contains("..") || txtBlurRatio.Text.Contains(".."))
                return false;

            if (txtNoiseValue.Text.Trim() == "" || txtXAxis.Text.Trim() == "" || txtYAxis.Text.Trim() == "" || txtBlurRatio.Text.Trim() == "" || txtAngle.Text.Trim() == "")
                return false;
            return true;
        }

        private void txtBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if ((sender as TextBox).Text.Length > 2)
                e.Handled = true;

            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        public string WindowHeading
        {
            get
            {
                if (AugType == EnumAugmentionType.Noise)
                    return "Noise Configuration";
                else if(AugType == EnumAugmentionType.Rotate)
                    return "Rotate Configuration";
                else if (AugType == EnumAugmentionType.Trans)
                    return "Translation Configuration";
                else
                    return "Blur Configuration";
            }
        }

        public Visibility IsVisibleNoiseConfig
        {
            get
            {
                if(AugType == EnumAugmentionType.Noise)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility IsVisibleRotationConfig
        {
            get
            {
                if (AugType == EnumAugmentionType.Rotate)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }
        public Visibility IsVisibleTransConfig
        {
            get
            {
                if (AugType == EnumAugmentionType.Trans)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility IsVisibleBlurConfig
        {
            get
            {
                if (AugType == EnumAugmentionType.Blur)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        private void btnClose_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void RadWindow_PreviewClosed(object sender, Telerik.Windows.Controls.WindowPreviewClosedEventArgs e)
        {
            if (!IsValidData())
            {
                MessageBox.Show("Please ensure that text fields were valid/not blank..!", "No Data", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
        }

        private void RadWindow_Closed(object sender, Telerik.Windows.Controls.WindowClosedEventArgs e)
        {
            app.settings.CurrentAugmentConfig.NoiseValue = Convert.ToDouble(txtNoiseValue.Text);
            app.settings.CurrentAugmentConfig.RotateDegree = Convert.ToInt16(txtAngle.Text);
            app.settings.CurrentAugmentConfig.Trans_Coordinate[0] = Convert.ToDouble(txtXAxis.Text);
            app.settings.CurrentAugmentConfig.Trans_Coordinate[1] = Convert.ToDouble(txtYAxis.Text);
            app.settings.CurrentAugmentConfig.BlurRatio = Convert.ToDouble(txtBlurRatio.Text);
        }
    }
}
