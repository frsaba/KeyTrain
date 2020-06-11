using KeyTrainWPF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Pythonic.ListHelpers;

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        static ChainMap<string, dynamic> CFG = ConfigManager.Settings;
        MainWindow owner;

        public SettingsWindow()
        {
            InitializeComponent();
        }
    }
}
