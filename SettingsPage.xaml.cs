using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.IO;
using KeyTrainWPF;
using static Pythonic.ListHelpers;

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public static ChainMap<string, dynamic> settings_copy;

        public SettingsPage()
        {
            InitializeComponent();
        }

        

        MainWindow window => (MainWindow)Window.GetWindow(this);

        private void dictFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose dictionary file(s)",
                Multiselect = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Resources"),
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };


            if (dialog.ShowDialog() == true)
            {
                settings_copy["dictionaryPath"] = dialog.FileNames.Select(p => new string[]
                    {p, Path.GetRelativePath(Directory.GetCurrentDirectory(), p)}   //Compare absolute and relative paths
                    .OrderBy(p => p.Count(c => c == '\\')).First()).ToList();       //Keep the simpler one

            }

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            window.LoadMainPage();
        }


        //TODO: hooks for each config value change: functions to call whenever a setting gets updated
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            settings_copy["lessonLength"] = (int)lengthslider.Value;
            ConfigManager.Settings = settings_copy;

            MainPage.Generator = RandomizedLesson.FromDictionaryFiles(ConfigManager.dictionaryPaths);
            MainPage.Text = MainPage.Generator.NextText();

            ConfigManager.WriteConfigFile();
            window.LoadMainPage(reset: true);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            settings_copy = ChainMap.FromList(ConfigManager.Settings.dicts);
            lengthslider.Value = settings_copy["lessonLength"];
        }
    }
}
