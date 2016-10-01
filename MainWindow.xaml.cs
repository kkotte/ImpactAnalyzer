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

namespace ImpactAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataPlotter _DataPlotter;
        ImpactAnalysisParams Params;

        public MainWindow()
        {
            InitializeComponent();

            _DataPlotter = new DataPlotter(Graph, Status);
            Params = ImpactAnalysisParams.GetDefaultImpactAnalysisParams();
        }

        private async void FileOpen_Clicked(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".csv";
            dlg.Filter = "Comma separated values (.csv)|*.csv";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Status.Text = "Opening \"" + dlg.SafeFileName + "\"";
                
                ProgressBar pbar = new ProgressBar();
                pbar.IsIndeterminate  = true;
                pbar.Height = 10;
                pbar.Width = 50;
                pbar.VerticalAlignment = VerticalAlignment.Center;
                StatusBar.Items.Add(pbar);
                DateTime StartTime = DateTime.Now;
                // List<Sample> list = await ImpactAnalyzerCore.GetSampleListFromFileAsync(Params, filename);
                List<Sample> impactData = await Task.Run<List<Sample>>(() => { return ImpactAnalyzerCore.GetSampleListFromFile(Params, filename); });

                Status.Text = "Analyzing \"" + dlg.SafeFileName + "\"";
                List<Activity> ActivityList = await Task.Run<List<Activity>>(() => { return ImpactAnalyzerCore.GetActivityList(Params, impactData); });
                
                Status.Text = "Parsed and analyzed \"" + dlg.SafeFileName + "\" (" + (DateTime.Now - StartTime).TotalSeconds + " seconds)";
                StatusBar.Items.Remove(pbar);

                /*
                if (false)
                {
                    Console.WriteLine("Writing analysis to file 'Analysis.txt'...");
                    using (StreamWriter writer = File.CreateText("Analysis.txt"))
                    {
                        for (int i = 0; i < ActivityList.Count; i++)
                        {
                            writer.WriteLine("Activity #{0} (Defined by impact [{1}, {2}))", i + 1, ActivityList[i].ImpactLowWaterMark, ActivityList[i].ImpactHighWaterMark);
                            writer.WriteLine("\tCount of impacts: {0}", ActivityList[i].SampleList.Count);
                            writer.WriteLine("\tFrequency of impacts: {0} per second", 1000.0 / ActivityList[i].AverageTimeDifference.TotalMilliseconds);
                            writer.WriteLine("\tDuration of activity: From [{0}] to [{1}]", ActivityList[i].ActivityStartTime().ToString("M/d/yyyy HH:mm:ss.FFF"), ActivityList[i].ActivityEndTime().ToString("M/d/yyyy HH:mm:ss.FFF"));
                        }
                    }

                }
                */

                _DataPlotter.ImpactData = impactData;
                _DataPlotter.ActivityData = new ActivityData(ActivityList, Params);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // A this point all controls have been loaded, so it should be safe to call these methods that rely on the dimensions of the _Graph canvas
            _DataPlotter.Draw();
        }

        private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _DataPlotter.Draw();
        }

        private void Window_KeyUp_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _DataPlotter.ZoomOut();
            }
        }

        private void Options_Clicked(object sender, RoutedEventArgs e)
        {
            OptionsDialog op = new OptionsDialog();

            op.Owner = this;
            op.Params = Params;
            op.ShowDialog();
        }
    }
}
