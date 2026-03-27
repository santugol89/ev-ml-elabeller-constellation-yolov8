using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GenieSupervisor
{
    public class ImageZoomBorder : Border
    {
        private UIElement child = null;
        private Point origin;
        private Point start;
        Cursor prevCursor;
        public double zoomX;
        public bool bWorkcellMode = false;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                //this.MouseRightButtonDown += child_MouseRightButtonDown;
                this.MouseMove += child_MouseMove;
                //this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(child_PreviewMouseRightButtonDown);
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                FrameworkElement drawingCanvas = this.child as FrameworkElement;
                // reset zoom
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);
                double defaultScale = 1.0;
                if (child.DependencyObjectType.Name == "Canvas")
                {
                    if(bWorkcellMode)
                        defaultScale = (this.ActualHeight / drawingCanvas.Height) - 0.005;
                    else
                        defaultScale = (this.ActualHeight / drawingCanvas.Height) - 0.05;

                    st.ScaleX = defaultScale;
                    st.ScaleY = defaultScale;

                    // reset pan
                    tt.X = 1;
                    tt.Y = 1;
                    zoomX = 0;
                }
                else if (child.DependencyObjectType.Name == "Image")
                {
                    st.ScaleX = defaultScale;
                    st.ScaleY = defaultScale;

                    tt.X = 0.0;
                    tt.Y = 0.0;
                }
                //UpdateShapeStrokeThickness(st);
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child == null)
                return;

            FrameworkElement drawingCanvas = child as FrameworkElement;
            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            // Smooth zoom (fine control)
            double zoomFactor = Math.Pow(1.0015, e.Delta);

            double minZoom, maxZoom;

            if (child is Canvas)
            {
                minZoom = bWorkcellMode
                    ? Math.Round((this.ActualHeight / drawingCanvas.Height) - 0.005, 2)
                    : Math.Round((this.ActualHeight / drawingCanvas.Height) - 0.05, 2);

                maxZoom = bWorkcellMode ? 0.9 : 6.2;
            }
            else // Image
            {
                minZoom = 1.0;
                maxZoom = 12.0;
            }

            double newScaleX = st.ScaleX * zoomFactor;
            double newScaleY = st.ScaleY * zoomFactor;

            // Clamp zoom range
            if (newScaleX < minZoom)
            {
                Reset();
                return;
            }

            if (newScaleX > maxZoom)
                return;

            Point relative = e.GetPosition(child);

            double absoluteX = relative.X * st.ScaleX + tt.X;
            double absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX = newScaleX;
            st.ScaleY = newScaleY;

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;

            if (child is Canvas)
                GetZoomStatus(e.Delta > 0 ? 0.1 : -0.1, st);
        }


        private void UpdateShapeStrokeThickness(System.Windows.Media.ScaleTransform st)
        {
            MainWindow mainApp = (MainWindow)Application.Current.MainWindow;
            mainApp.ShapeStrokeThickness = st.ScaleX > 0 && st.ScaleX <= 2 ? 4 : st.ScaleX > 2 && st.ScaleX <= 4 ? 2 : 1;

            Canvas curCanvas = this.Child as Canvas;
            for (int i = 1; i < curCanvas.Children.Count; i++)
            {
                Shape curShapes = curCanvas.Children[i] as Shape;
                curShapes.StrokeThickness = mainApp.ShapeStrokeThickness;
            }
        }

        private void GetZoomStatus(double zoom, System.Windows.Media.ScaleTransform st)
        {
            Grid gridZoom = this.Parent as Grid;
            Label lblZoom = gridZoom.Children[1] as Label;

            double offset = bWorkcellMode ? 0 : 0.2;
            if (zoom >= 0)
            {
                zoomX += zoom + offset;
                lblZoom.Content = "Zoomed in to Level : " + zoomX.ToString("0.0") + "X";
            }
                
            else if (zoom < 0 && zoomX > 0.5)
            {
                zoomX += zoom - offset;
                lblZoom.Content = "Zoomed out to Level : " + zoomX.ToString("0.0") + "X";
            }

            else if (zoom < 0 && zoomX < 1)
                lblZoom.Content = "Zoomed to Original size";

            lblZoom.Visibility = Visibility.Visible;
            Storyboard sb = Application.Current.MainWindow.Resources["sbHideZoomLabel"] as Storyboard;
            sb.Begin(lblZoom);
        }
         
        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                prevCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                child.ReleaseMouseCapture();
            }
            Mouse.OverrideCursor = prevCursor;
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private void child_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (child != null && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (child.IsMouseCaptured)
                {
                    var st = GetScaleTransform(child);

                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);

                    Point cornerTL = new Point(origin.X - v.X, origin.Y - v.Y);
                    Point cornerBR = new Point((origin.X - v.X) + ActualWidth * st.ScaleX, (origin.Y - v.Y) + ActualHeight * st.ScaleY);

                    Point marginTL = new Point(0, 0);
                    Point marginBR = new Point(ActualWidth, ActualHeight);

                    if (st.ScaleX > 0.3 || st.ScaleY > 0.3)
                    {
                        tt.X = origin.X - v.X;
                        tt.Y = origin.Y - v.Y;
                    }
                }
            }
        }
        private void child_MouseRightButtonDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Reset();
        }

        #endregion
    }
}

