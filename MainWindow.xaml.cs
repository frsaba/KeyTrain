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

namespace KeyTrain
{
   
    public partial class MainWindow : Window
    {
        public MainPage mainPage { get; private set; } = new MainPage();
        public SettingsPage settingsPage { get; private set; } = new SettingsPage();
        public StatsPage statsPage { get; private set; } = null;
        ChainMap<string,dynamic> CFG => ConfigManager.Settings;

        Dictionary<string, object> windowPlacementBindings => new Dictionary<string, object>
            {
                {"windowWidth" , Width},
                {"windowHeight", Height},
                {"windowState" , WindowState.ToString()},
                {"windowLeft" ,  Left},
                {"windowTop",    Top}
            };

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
            foreach (var pair in windowPlacementBindings)
            {
                CFG[pair.Key] = pair.Value;
            }
            mainPage.Window_Closing(sender, e);
        }

        public void LoadMainPage(bool reset = false, bool reEmphasize = true)
        {
            Frame.Content = mainPage;
            
            if (reEmphasize && MainPage.Generator.GetType() == typeof(RandomizedLesson))
            {
                ((RandomizedLesson)MainPage.Generator).Emphasize(MainPage.selectedChars);
                MainPage.Text = MainPage.Generator.NextText();
            }
            if (reset)
            {
                mainPage.Reset();
            }
            mainPage.RatingsChanged();
        }
        public void LoadSettingsPage()
        {
            Frame.Content = settingsPage;
        }

        public void LoadStatsPage()
        {
            statsPage ??= new StatsPage();
            Frame.Content = statsPage;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            //Making sure both mainRealEstate's margins and letterRatings' columns behave properly on maximize
            mainPage.RatingsChanged();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            if (windowPlacementBindings.Keys.All(k => CFG.Keys.Contains(k)))
            {
                Left = CFG["windowLeft"];
                Top = CFG["windowTop"];
                WindowState = Enum.Parse(typeof(WindowState), CFG["windowState"]);
                if (CFG["windowState"] == "Normal")
                {
                    Width = CFG["windowWidth"];
                    Height = CFG["windowHeight"];
                }
            }

        }
    }
}
