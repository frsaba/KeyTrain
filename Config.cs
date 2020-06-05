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
        public static string profilePath => Settings["profilePath"];
        public static dynamic dictionaryPaths { get => Settings["dictionaryPath"]; set => Settings["dictionaryPath"] = value; }

        static Dictionary<string, dynamic> defaultSettings = new Dictionary<string, dynamic>(){
            {"lessonLength", 100 },
            {"profilePath", "Profile/profile.kts" }

        };
        static Dictionary<string, dynamic> userSettings = new Dictionary<string, dynamic>();
        static Dictionary<string, dynamic> styleSheet = new Dictionary<string, dynamic>();
        public static ChainMap<string, dynamic> Settings { get; private set; } = 
            ChainMap.FromDicts(userSettings, styleSheet, defaultSettings); 

        

        public static void ReadConfigFile(string path = "KeyTrain.cfg")
        {
            foreach (string line in File.ReadAllLines(path)
            //ignore empty or comments
            .Where(l => (!string.IsNullOrWhiteSpace(l)) || l.StartsWith("//") ))
            {
                var split = line.Split(":",2);
                string key = split[0].Trim();
                string value = split[1].Trim();

                MatchCollection matches = Regex.Matches(value, @"(?<=('\b))(?:(?=(\\?))\2.)*?(?=\1)"); //Wrapped in '' -> string
                if (matches.Count > 0) 
                {
                    if(matches.Count == 1) //single string
                    {
                        Trace.WriteLine($"{key} set single - {matches[0].Groups[0].Value}");
                        Settings[key] = matches[0].Groups[0].Value;
                    }
                    else //multiple values -> make a list of them
                    {
                        Settings[key] = matches.Select(m => m.Groups[0].Value);
                    }
                    
                    
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

        public static void WriteConfigFile(string path = "KeyTrain.cfg")
        {
            foreach (var item in userSettings)
            {
                string result;
                var v = item.Value;
                if (typeof(string).IsAssignableFrom(v))
                {
                    result = $"\"{v}\"";
                }
                else
                {
                    result = v.ToString();
                }

                Trace.WriteLine($"{item.Key}: {result}");
            }
        }
    }
}
