using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KeyTrainWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Slider_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
                Slider s = sender as Slider;
                s.Value += Math.Sign(e.Delta) * s.TickFrequency;
        }
    }
}
