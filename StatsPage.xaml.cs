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
            for (int i = smoothness; i < wpmlog.Count - smoothness; i++)
            {
                points.Add(new ScatterPoint(i,Math.Round( wpmlog.Skip(i).Take(smoothness).Average(), 2)));
            }
            
            wpmline.ItemsSource = points;
            wpmplot.InvalidatePlot(false);
        }

        void Refresh()
        {
            var smoothness = (int)wpmSmoothSlider.Value;
            var points = new List<DataPoint>();
            for (int i = smoothness; i < wpmlog.Count - smoothness; i++)
            {
                points.Add(new DataPoint(i, Math.Round(wpmlog.Skip(i).Take(smoothness).Average(), 2)));
            }
        }
    }
}
