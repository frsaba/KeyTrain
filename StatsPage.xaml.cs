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

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for StatsPage.xaml
    /// </summary>
    public partial class StatsPage : Page
    {
        MainWindow window;

        List<double> wpmlog => MainPage.stats.WPMLOG;
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
            wpmplot.Title = "WPM";
            var xaxis = wpmplot.Axes.ElementAt(0);
            var yaxis = wpmplot.Axes.ElementAt(1);
            yaxis.Title = "WPM";
            xaxis.Title = "Sample";
            if(wpmlog.Count > 0)
            {
                xaxis.Maximum = wpmlog.Count;
                yaxis.Minimum = wpmlog.Min();
                yaxis.Maximum = wpmlog.Max();

            }
            wpmplot.InvalidatePlot(false);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        { 

            var smoothness = (int)e.NewValue;
            var points = new List<ScatterPoint>();
            int start = smoothness;
            int end = wpmlog.Count - smoothness;
            int step = (int)Math.Ceiling(wpmlog.Count / 1000.0);
            double avg = wpmlog.Average();
            for (int i = start; i < end; i+=step)
            {
                var x = Math.Round(((double)i - smoothness) / (end - start) * wpmlog.Count);
                points.Add(new ScatterPoint(
                    x: x,
                    y: Math.Round(wpmlog.Skip(i - smoothness).Take(smoothness + 1).Average(), 2),
                    size:1 + MainPage.stats.MISSLOG[(int)x] / 1.4) );
            }
            
            wpmline.ItemsSource = points;
            var xaxis = wpmplot.Axes.ElementAt(0);
            var yaxis = wpmplot.Axes.ElementAt(1);
            xaxis.Minimum = points.Min(p => p.X) - 10;
            xaxis.Maximum = points.Max(p => p.X) + 10;
            xaxis.AbsoluteMinimum = -points.Count / 2;
            yaxis.Minimum = points.Min(p => p.Y) - 1;
            yaxis.Maximum = points.Max(p => p.Y) + 1;
            wpmplot.InvalidatePlot(true);
        }

    }
}
