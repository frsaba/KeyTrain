using KeyTrain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using System.Linq;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows.Input;

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for StatsPage.xaml
    /// </summary>
    public partial class StatsPage : Page
    {
        MainWindow window;

        List<double> wpmlog => MainPage.stats.WPMLOG;
        List<int> misslog => MainPage.stats.MISSLOG;
        public StatsPage()
        {
            InitializeComponent();

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            window.LoadMainPage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            window = (MainWindow)Window.GetWindow(this);
            Keyboard.Focus(wpmplot);
            wpmplot.Title = "WPM";
            var xaxis = wpmplot.Axes.ElementAt(0);
            var yaxis = wpmplot.Axes.ElementAt(1);
            yaxis.Title = "WPM";
            xaxis.Title = "Sample";

            wpmSmoothSlider.Maximum = (int)Math.Max(5, Math.Min(800, Math.Floor(wpmlog.Count / 2.0) - 1));

            if (wpmlog.Count > 0)
            {
                xaxis.Maximum = wpmlog.Count;
                yaxis.Minimum = wpmlog.Min();
                yaxis.Maximum = wpmlog.Max();

            }
            UpdateWPMChart();
        }

        private void UpdateWPMChart()
        {
            if (wpmlog.Count > 1)
            {

                var smoothness = (int)Math.Min(wpmSmoothSlider.Value, Math.Floor(wpmlog.Count / 2.0) - 1);
                var wpmpoints = new List<ScatterPoint>();
                var misspoints = new List<ScatterPoint>();
                var endpoints = new List<DataPoint>();
                int start = smoothness;
                int end = wpmlog.Count - smoothness;
                int step = Math.Min(smoothness + 1, (int)Math.Ceiling(wpmlog.Count / 1000.0));
                double avg = wpmlog.Average();
                for (int i = start; i < end; i += step)
                {
                    var x = Math.Round(((double)i - smoothness) / (end - start) * wpmlog.Count);
                    wpmpoints
                        .Add(new ScatterPoint(
                        x: x,
                        y: Math.Round(wpmlog.Skip(i - smoothness).Take(smoothness + 1).Average(), 2),
                        size: 1 + MainPage.stats.MISSLOG[(int)x] / 1.4));
                    misspoints
                        .Add(new ScatterPoint(
                        x: x,
                        y: Math.Round(misslog.Skip(i - smoothness).Take(smoothness + 1).Average() * (wpmlog.Max() / misslog.Max()), 2) + wpmlog.Min()));

                }

                endpoints.Add(new DataPoint(0, Math.Round(wpmlog.Take(wpmlog.Count / 10).Average(), 2))); //average of first 10%
                endpoints.Add(new DataPoint(wpmlog.Count, Math.Round((wpmlog as IEnumerable<double>).Reverse().Take(wpmlog.Count / 10).Average(), 2))); //average of last 10%

                wpmline.ItemsSource = wpmpoints;
                missesline.ItemsSource = misspoints;
                endpointseries.ItemsSource = endpoints;
                var xaxis = wpmplot.Axes.ElementAt(0);
                var yaxis = wpmplot.Axes.ElementAt(1);
                xaxis.Minimum = wpmpoints.Min(p => p.X) - 10;
                xaxis.Maximum = wpmpoints.Max(p => p.X) + 10;
                xaxis.AbsoluteMinimum = -wpmpoints.Count / 2;
                yaxis.Minimum = misspoints.Min(p => p.Y) - 1;
                yaxis.Maximum = wpmpoints.Max(p => p.Y) + 1;
                wpmplot.InvalidatePlot(true);

            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateWPMChart();
            
        }

        private void Page_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if(e.Key == Key.Escape)
            {
                window.LoadMainPage();
            }
        }
    }
}
