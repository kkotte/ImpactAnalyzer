using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Reflection;

namespace ImpactAnalyzer
{
    class DataPlotter
    {
        private Canvas _Graph; // Where everything is plotted
        private Canvas _Chart; // Where the impact data is plotted (does not include x and y axis labels)
        // private Canvas _ActivityChart; // Where activity analysis is plotted
        private TextBlock _Status;
        private Rectangle ZoomRectangle;
        private Polyline _Polyline;
        private Polyline _AverageLine;

        private List<Sample> _ImpactData;
        public List<Sample> ImpactData
        {
            get { return _ImpactData; }
            set
            {
                _ImpactData = value;

                _Chart.IsEnabled = true;

                ImpactDataStartTime = _ImpactData[0].Time.Ticks;
                ImpactDataEndTime   = _ImpactData[_ImpactData.Count - 1].Time.Ticks;

                MinXInTicks = ImpactDataStartTime;
                MaxXInTicks = ImpactDataEndTime;

                MaxY = _ImpactData.Max(sample => sample.Value);
                MinY = _ImpactData.Min(sample => sample.Value);

                Draw();
            }
        }

        private List<Activity> _ActivityList;
        private ImpactAnalysisParams _Params;
        public ActivityData ActivityData
        {
            set
            {
                _ActivityList = value.ActivityList;
                _Params = value.Params;

                // Get activity count
                ActivityCount = _Params.ActivityDefinitionList.Count;
                ActivityChartHeight = ActivityRawDataSpacer + ActivityCount * (ActivityLineThickness + ActivityLineSpacing);

                Draw();
            }
        }

        private long ImpactDataStartTime { get; set; }
        private long ImpactDataEndTime { get; set; }

        private long MinXInTicks { get; set; }
        private long MaxXInTicks { get; set; }
        
        public double MinY { get; set; }
        public double MaxY { get; set; }
        private double MinYAxis { get; set; }
        private double MaxYAxis { get; set; }

        static private int XAxisLabelWidth = 50;
        static private int YAxisLabelHeight = 40;
        static private double YAxisMultiple = 1;
        static private int XAxisGuideLines = 10;
        static private double ZoomPercent = 10;
        static private double ActivityLineThickness = 5;
        static private double ActivityLineSpacing = 15;
        static private double ActivityRawDataSpacer = 25;

        static private int ActivityCount = 0;
        static private double ActivityChartHeight = 0;
        static private double RawPlotHeight = 0;

        private bool IsZooming { get; set; }
        private double ZoomStart { get; set; }

        public DataPlotter(Canvas graph, TextBlock status)
        {
            MinY = -6;
            MaxY = 1;
            MinXInTicks = new DateTime().Ticks;
            MaxXInTicks = new DateTime().AddHours(1).Ticks; // 1 hour
            
            _Graph = graph;
            _Graph.Background = Brushes.White;
            _Status = status;
            IsZooming = false;

            // Create the region for the chart
            _Chart = new Canvas();
            _Chart.Background = Brushes.LightBlue;
            Canvas.SetLeft(_Chart, XAxisLabelWidth);
            Canvas.SetTop(_Chart, 0);
            _Chart.Cursor = Cursors.IBeam;
            _Chart.MouseLeftButtonDown += _Chart_MouseLeftButtonDown;
            _Chart.MouseLeftButtonUp += _Chart_MouseLeftButtonUp;
            _Chart.MouseMove += _Chart_MouseMove;
            _Chart.MouseWheel += _Chart_MouseWheel;

            _Chart.IsEnabled = false; // Disabled to start off with

            // Create the zoom rectangle
            ZoomRectangle = new Rectangle();
            ZoomRectangle.Fill = Brushes.WhiteSmoke;
            ZoomRectangle.Opacity = 0.5;
            Canvas.SetTop(ZoomRectangle, 0);

            // Create the polyline
            _Polyline = new Polyline();
            _Polyline.Stroke = Brushes.Black;
            _Polyline.StrokeThickness = 1;
            Canvas.SetLeft(_Polyline, 0);
            Canvas.SetTop(_Polyline, 0);

            //Create the 'average' line
            _AverageLine = new Polyline();
            _AverageLine.Stroke = Brushes.Red;
            _AverageLine.StrokeThickness = 1;
            Canvas.SetLeft(_AverageLine, 0);
            Canvas.SetTop(_AverageLine, 0);
        }

        public void ZoomOut()
        {
            MinXInTicks = ImpactDataStartTime;
            MaxXInTicks = ImpactDataEndTime;

            Draw();
        }

        void _Chart_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            
            long NewMinXInTicks = MinXInTicks;
            long NewMaxXInTicks = MaxXInTicks;

            long delta = (long)((MaxXInTicks - MinXInTicks) * ZoomPercent / 100);

            if (e.Delta > 0)
            {
                // Zoom in
                NewMinXInTicks += delta;
                NewMaxXInTicks -= delta;
            }
            else
            {
                // Zoom out
                NewMinXInTicks -= delta;
                NewMaxXInTicks += delta;
            }

            if (NewMinXInTicks > NewMaxXInTicks)
            {
                NewMinXInTicks = NewMaxXInTicks;
            }

            NewMinXInTicks = Math.Max(NewMinXInTicks, ImpactDataStartTime);
            NewMaxXInTicks = Math.Min(NewMaxXInTicks, ImpactDataEndTime);

            MinXInTicks = NewMinXInTicks;
            MaxXInTicks = NewMaxXInTicks;

            Draw();
        }

        void _Chart_MouseMove(object sender, MouseEventArgs e)
        {
            double currentMousePos = Math.Max(0, e.GetPosition(_Chart).X);
            currentMousePos = Math.Min(currentMousePos, _Chart.ActualWidth);
            if (e.LeftButton == MouseButtonState.Pressed && IsZooming)
            {
                _Status.Text = "Selecting from [" + XCoordToLabel(Math.Min(currentMousePos, ZoomStart)) + " (" + GetValue(XCoordToTicks(Math.Min(currentMousePos, ZoomStart))).ToString("0.000") + ")] to [" + XCoordToLabel(Math.Max(currentMousePos, ZoomStart)) + " (" + GetValue(XCoordToTicks(Math.Max(currentMousePos, ZoomStart))).ToString("0.000") + ")]";
                ZoomRectangle.Width = Math.Abs(currentMousePos - ZoomStart);
                Canvas.SetLeft(ZoomRectangle, Math.Min(currentMousePos, ZoomStart));
            }
            else
            {
                _Status.Text = "Time: [" + XCoordToLabel(currentMousePos) + " (" + GetValue(XCoordToTicks(currentMousePos)).ToString("0.000") + ")]";
            }
        }

        void _Chart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsZooming)
            {
                double currentMousePos = Math.Max(0, e.GetPosition(_Chart).X);
                currentMousePos = Math.Min(currentMousePos, _Chart.ActualWidth);
                _Status.Text = "Zooming between [" + XCoordToLabel(Math.Min(currentMousePos, ZoomStart)) + "] and [" + XCoordToLabel(Math.Max(currentMousePos, ZoomStart)) + "]";
                _Chart.ReleaseMouseCapture();
                IsZooming = false;

                long NewMinXInTicks = XCoordToTicks(Math.Min(currentMousePos, ZoomStart));
                long NewMaxXInTicks = XCoordToTicks(Math.Max(currentMousePos, ZoomStart));

                ZoomRectangle.Visibility = Visibility.Hidden;

                if (NewMinXInTicks != NewMaxXInTicks) // Skip double clicks
                {
                    MinXInTicks = NewMinXInTicks;
                    MaxXInTicks = NewMaxXInTicks;

                    Draw();
                }
            }
        }

        void _Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsZooming = true;
            ZoomStart = e.GetPosition(_Chart).X;
            _Status.Text = "Dragging from [" + XCoordToLabel(ZoomStart) + "]";
            Canvas.SetLeft(ZoomRectangle, ZoomStart);
            ZoomRectangle.Width = 0;
            ZoomRectangle.Visibility = Visibility.Visible;
            _Chart.CaptureMouse();
        }

        public void Draw()
        {
            _Chart.Children.RemoveRange(0, _Chart.Children.Count);
            _Graph.Children.RemoveRange(0, _Graph.Children.Count);

            _Chart.Height = _Graph.ActualHeight - YAxisLabelHeight /*- ActivityChartHeight*/;
            _Chart.Width = _Graph.ActualWidth - XAxisLabelWidth;
            RawPlotHeight = _Chart.Height - ActivityChartHeight;

            ZoomRectangle.Height = _Chart.Height /*+ _ActivityChart.Height*/;

            _Graph.Children.Add(_Chart);
            _Chart.Children.Add(ZoomRectangle);
            _Chart.Children.Add(_Polyline);
            _Chart.Children.Add(_AverageLine);
            
            DrawAxes();
            PlotImpactData();
            PlotActivityData();
        }

        public void DrawAxes()
        {
            
            // Draw Y axis
            MinYAxis = (Math.Floor(Math.Abs(MinY) / YAxisMultiple) + 1) * YAxisMultiple * (MinY < 0 ? -1 : 1);
            MaxYAxis = (Math.Round(MaxY / YAxisMultiple) + 1) * YAxisMultiple;

            // Draw lines
            for (double i = MinYAxis; i <= MaxYAxis; i += YAxisMultiple)
            {
                // Draw the line
                double Y = RawPlotHeight - (i - MinYAxis) * RawPlotHeight / (MaxYAxis - MinYAxis);
                _Chart.Children.Add(GetLine(0, Y, _Chart.Width, Y));

                // Draw the label
                TextBox tb = new TextBox();
                tb.Text = string.Format("{0:0.0}", i);
                tb.FontWeight = FontWeights.Light;
                tb.BorderThickness = new Thickness(0);
                tb.Background = _Graph.Background;
                Canvas.SetRight(tb, _Chart.Width + 1);
                _Graph.Children.Add(tb);
                tb.UpdateLayout();
                Canvas.SetTop(tb, Y - tb.ActualHeight / 2); // Has to be done after it has been added to the parent canvas and UpdateLayout has been called
            }

            // Draw (XAxisGuideLines+1) additional lines - This will ensure that we have XAxisGuideLines lines between the first and the last datapoints
            for (int i = 0; i < XAxisGuideLines + 1; i++)
            {
                double X = i * _Chart.Width / (XAxisGuideLines + 1);
                _Chart.Children.Add(GetLine(X, 0, X, RawPlotHeight)); 

                // Draw the label
                TextBox tb = new TextBox();
                tb.Text = XCoordToLabel(X);
                tb.FontWeight = FontWeights.Light;
                tb.BorderThickness = new Thickness(0);
                tb.Background = _Graph.Background;
                Canvas.SetTop(tb, _Chart.Height /*+ _ActivityChart.Height*/);
                _Graph.Children.Add(tb);
                tb.UpdateLayout();
                Canvas.SetLeft(tb, X + XAxisLabelWidth - tb.ActualWidth); // Has to be done after it has been added to the parent canvas and UpdateLayout has been called
                tb.RenderTransform = new RotateTransform(-45, tb.ActualWidth, 0);
            }

            // Draw the last X axis guide line
            _Chart.Children.Add(GetLine(_Chart.Width, 0, _Chart.Width, RawPlotHeight));
            TextBox tb1 = new TextBox();
            tb1.Text = XCoordToLabel(_Chart.Width);
            tb1.FontWeight = FontWeights.Light;
            tb1.BorderThickness = new Thickness(0);
            tb1.Background = _Graph.Background;
            Canvas.SetTop(tb1, _Chart.Height /*+ _ActivityChart.Height*/);
            _Graph.Children.Add(tb1);
            tb1.UpdateLayout();
            Canvas.SetLeft(tb1, _Chart.Width + XAxisLabelWidth - tb1.ActualWidth); // Has to be done after it has been added to the parent canvas and UpdateLayout has been called
            tb1.RenderTransform = new RotateTransform(-45, tb1.ActualWidth, 0);

            // Draw Activity labels
            for (int i = 0; i < ActivityCount; i ++)
            {
                // Draw the label
                TextBox tb = new TextBox();
                tb.Text = _Params.ActivityDefinitionList[i].Name;
                tb.FontWeight = FontWeights.Light;
                tb.BorderThickness = new Thickness(0);
                tb.Background = _Graph.Background;
                Canvas.SetRight(tb, _Chart.Width + 1);
                _Graph.Children.Add(tb);
                tb.UpdateLayout();
                Canvas.SetTop(tb, RawPlotHeight + ActivityRawDataSpacer + i * (ActivityLineThickness + ActivityLineSpacing) - 2 * ActivityLineThickness); // Has to be done after it has been added to the parent canvas and UpdateLayout has been called
            }


        }

        private Line GetLine(double X1, double Y1, double X2, double Y2)
        {
            Line l = new Line();
            l.X1 = X1; l.X2 = X2; l.Y1 = Y1; l.Y2 = Y2;
            l.Stroke = Brushes.Gray;
            l.StrokeThickness = 0.5;
            return l;
        }

        private long XCoordToTicks(double X)
        {
            return MinXInTicks + (long)((MaxXInTicks - MinXInTicks) * X / _Chart.Width);
        }

        private string XCoordToLabel(double X)
        {
            return new DateTime(XCoordToTicks(X)).ToString("h:mm:ss.ff t");
        }

        private double GetValue(long tick)
        {
            DateTime time = new DateTime(tick);
            double retVal = 0.0;

            if (_ImpactData != null)
            {
                Sample s = ImpactData.Find(sample => (sample.Time >= time));
                if (s != null)
                {
                    retVal = s.Value;
                }
            }
            return retVal;
        }

        private void PlotImpactData()
        {
            DateTime ViewStartTime = new DateTime(MinXInTicks);
            DateTime ViewEndTime = new DateTime(MaxXInTicks);

            PointCollection pc = new PointCollection();
            PointCollection average = new PointCollection();

            if (_ImpactData != null)
            {
                int start = ImpactData.FindIndex(sample => (sample.Time >= ViewStartTime));
                int end = ImpactData.FindIndex(sample => (sample.Time >= ViewEndTime));

                if (start > end)
                {
                    start = end;
                }

                List<Sample> ViewList = ImpactData.GetRange(start, end - start + 1);
                if (_Chart.Width * 2 >= ViewList.Count)
                {
                    ViewList.ForEach(sample =>
                    {
                        pc.Add(GetPoint(sample));
                        average.Add(GetPoint(sample.Time.Ticks, sample.Average));
                    });
                }
                else 
                {
                    // Draw polyline
                    int index1, index2 = 0;
                    
                    for (double i = 0; i < _Chart.Width; i++)
                    {
                        DateTime ts = new DateTime(XCoordToTicks(i));

                        index1 = index2;
                        index1 = ImpactData.FindIndex(index2, sample => (sample.Time <= ts));
                        index2 = ImpactData.FindIndex(index1, sample => (sample.Time >= ts));

                        double v;

                        // Interpolate
                        if (ImpactData[index1].Time.Ticks != ImpactData[index2].Time.Ticks)
                        {
                            v = ImpactData[index1].Value + (ts - ImpactData[index1].Time).Ticks * (ImpactData[index2].Value - ImpactData[index1].Value) / (ImpactData[index2].Time - ImpactData[index1].Time).Ticks;
                        }
                        else
                        {
                            v = ImpactData[index1].Value;
                        }

                        pc.Add(GetPoint(ts.Ticks, v));
                        average.Add(GetPoint(ts.Ticks, _ImpactData[index1].Average));
                    }

                    // Draw cumulative impact data per pixel
                    int index = 0;
                    double UpSampling = 1;
                    double NumPixels = _Chart.Width * UpSampling;
                    double ImpactPointsPerPixel = ViewList.Count / NumPixels;

                    double max, min;

                    for (double i = 0; i < NumPixels; i++)
                    {
                        if (index < (i + 1) * ImpactPointsPerPixel &&
                            index < ViewList.Count - 1)
                        {
                            max = ViewList[index].Value;
                            min = ViewList[index].Value;
                            index++;
                        }
                        else
                        {
                            max = 0;
                            min = 0;
                        }

                        while (index < (i + 1) * ImpactPointsPerPixel &&
                               index < ViewList.Count - 1)
                        {
                            max = Math.Max(max, ViewList[index].Value);
                            min = Math.Min(min, ViewList[index].Value);
                            index++;
                        }
                        
                        _Chart.Children.Add(GetLine(i / UpSampling, min, max));
                    }
                }

            }

            _Polyline.Points = pc;
            _AverageLine.Points = average;
        }

        private void PlotActivityData()
        {
            string[] ActivityColor = new string[] {"Green", "Orange", "Maroon", "Red", "Blue"};
            var ColorMap = typeof(Brushes).GetProperties().Select(p => new { Name = p.Name, Brush = p.GetValue(null) as Brush });
            if (_ActivityList != null)
            {
                foreach (Activity activity in _ActivityList)
                {
                    double y = RawPlotHeight + ActivityRawDataSpacer + activity.ActivityId * (ActivityLineSpacing + ActivityLineThickness);
                    Line l = new Line();
                    l.Y1 = l.Y2 = y;
                    l.X1 = TicksToXCoord(Math.Max(activity.ActivityStartTime().Ticks, MinXInTicks));
                    l.X2 = TicksToXCoord(Math.Min(activity.ActivityEndTime().Ticks, MaxXInTicks));
                    
                    // Ensure that activity lines can be seen
                    if (l.X2 - l.X1 < 5)
                    {
                        l.X1 -= (5 - (l.X2 - l.X1)) / 2;
                        l.X2 += (5 - (l.X2 - l.X1)) / 2;
                    }

                    string c = ActivityColor[activity.ActivityId];
                    l.Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(c);
                    l.StrokeThickness = ActivityLineThickness;
                    l.ToolTip = string.Format("Activity: {0} (defined by impact [{1}, {2}))\r\nCount of impacts: {3}\r\nFrequency of impacts: {4} per second\r\nDuration: From [{5}] to [{6}]",
                                activity.Definition.Name, activity.Definition.ImpactLowWaterMark, activity.Definition.ImpactHighWaterMark,
                                activity.SampleList.Count, 1000.0 / activity.AverageTimeDifference.TotalMilliseconds, 
                                activity.ActivityStartTime().ToString("M/d/yyyy HH:mm:ss.FFF"), activity.ActivityEndTime().ToString("M/d/yyyy HH:mm:ss.FFF"));
                    l.Cursor = Cursors.Arrow;
                    _Chart.Children.Add(l);
                }
            }
        }

        private double TicksToXCoord(long ticks)
        {
            return _Chart.Width * (ticks - MinXInTicks) / (MaxXInTicks - MinXInTicks);
        }

        private Point GetPoint(Sample sample)
        {
            return GetPoint(sample.Time.Ticks, sample.Value);
        }

        private Point GetPoint(long ticks, double value)
        {
            double X, Y;

            X = TicksToXCoord(ticks);
            Y = ComputeYCoord(value);

            return new Point(X, Y);
        }

        private double ComputeYCoord(double value)
        {
            return RawPlotHeight - (RawPlotHeight * (value - MinYAxis) / (MaxYAxis - MinYAxis));
        }

        private Line GetLine(double xCoord, double yMinCoord, double yMaxCoord)
        {
            Line l = new Line();
            l.X1 = l.X2 = xCoord;
            l.Y1 = ComputeYCoord(yMinCoord);
            l.Y2 = ComputeYCoord(yMaxCoord);
            l.Stroke = Brushes.Black;
            l.StrokeThickness = 1;
            return l;
        }
    }

}
