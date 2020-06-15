using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using KeyTrain;
using Pythonic;
using static Pythonic.ListHelpers;
using static KeyTrain.KeyTrainStatsConversion;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Dynamic;
using System.Threading;
using System.ComponentModel;
//using static KeyTrain.DarkStyles.MainWindow;
using Microsoft.Win32;
using System.IO;

namespace KeyTrainWPF
{
   
    public partial class MainWindow : Window
    {
        public MainPage mainPage { get; private set; } = new MainPage();
        public SettingsPage settingsPage { get; private set; } = new SettingsPage();


        public MainWindow()
        {
            InitializeComponent();
            ConfigManager.ReadConfigFile();
            Focusable = true;
            NavigationCommands.BrowseBack.InputGestures.Clear();
            LoadMainPage(reset: true);
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            mainPage.Window_Closing(sender, e);
        }

        public void LoadMainPage(bool reset = false)
        {
            Frame.Content = mainPage;
            if (reset)
            {
                mainPage.Reset();
            }
        }
        public void LoadSettingsPage()
        {
            Frame.Content = settingsPage;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            mainPage.RatingsChanged();
        }

        //private void SettingsButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if(Frame.Content == mainPage)
        //    {
        //        Frame.Content = settingsPage;
        //    }
        //    else
        //    {
        //        Frame.Content = mainPage;
        //    }
        //}
    }
}
