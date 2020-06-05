using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using static Pythonic.ListHelpers;
using System.Windows.Media;
using System.Diagnostics;

namespace KeyTrain
{

    public static class ConfigManager
    {
        static Dictionary<string, dynamic> defaultSettings = new Dictionary<string, dynamic>(){
            {"lessonLength", 50 },
            {"profilePath", "Profile/profile.kts" }

        };
        static Dictionary<string, dynamic> userSettings = new Dictionary<string, dynamic>();
        static Dictionary<string, dynamic> styleSheet = new Dictionary<string, dynamic>();
        public static ChainMap<string, dynamic> Settings { get; private set; } = 
            ChainMap.FromDicts(userSettings, styleSheet, defaultSettings); 

        

        public static void ReadConfigFile(string path = "keytrain.cfg")
        {
            foreach (string line in File.ReadAllLines(path)
            //ignore empty or comments
            .Where(l => (!string.IsNullOrWhiteSpace(l)) || l.StartsWith("//") ))
            {
                var split = line.Split(":",1);
                string key = split[0].Trim();
                string value = split[1].Trim();

                Match match = Regex.Match(value, "^\"(.*)\"$"); //Wrapped in "" -> string
                if (match.Success) 
                {
                    Settings[key] = match.Groups[0];
                }
                else if(int.TryParse(value, out int result))
                {
                    Settings[key] = result;
                }
                else
                {
                    try
                    {
                        Settings[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
                    }
                    catch(FormatException)
                    {
                        Trace.WriteLine($"Unrecognized key '{key}' in ConfigManager; it will be ignored.");
                    }
                }
                
            }
        }

    }
}
