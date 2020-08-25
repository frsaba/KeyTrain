using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using static Pythonic.ListHelpers;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;

namespace KeyTrain
{

    public static class ConfigManager
    {
        public static string profilePath => Settings["profilePath"];
        public static dynamic dictionaryPaths { get => Settings["dictionaryPath"]; set => Settings["dictionaryPath"] = value; }
        public static int  lessonLength { get => Settings["lessonLength"]; set => Settings["lessonLength"] = value; }

        static Dictionary<string, dynamic> defaultSettings = new Dictionary<string, dynamic>(){
            {"lessonLength", 100 },
            {"profilePath", "Profile/profile.kts" },
            {"dictionaryPath", "Resources/dictionaryEN.txt" },
            {"capitalsLevel", 2 },
            {"generator", "random" },
            {"presetText", "Lorem ipsum dolor sit amet" },
            {"emphasizedLetters", "" }//0: force lower, 1: keep existing, 2: half-half 3: first letter, 4: all caps
        };
        static Dictionary<string, dynamic> userSettings = new Dictionary<string, dynamic>();
        static Dictionary<string, dynamic> styleSheet = new Dictionary<string, dynamic>();
        public static ChainMap<string, dynamic> Settings { get; set; } = 
            ChainMap.FromDicts(userSettings, styleSheet, defaultSettings); 

        

        public static void ReadConfigFile(string path = "KeyTrain.cfg")
        {
            if (File.Exists(path) == false)
            {
                Trace.WriteLine("No configuration file found");
                return;
            }
            foreach (string line in File.ReadAllLines(path)
            //ignore empty or comments
            .Where(l => (!string.IsNullOrWhiteSpace(l)) || l.StartsWith("//") ))
            {
                try{
                    var split = line.Split(":", 2);
                    string key = split[0].Trim();
                    string value = split[1].Trim();

                    if(value.StartsWith("'")) // string value
                    {
                        //makes a list all values inside ''
                        var result = new List<string>();
                        int start = 1;

                        do
                        {
                            int end = value.IndexOf("'", start);
                            string next = value.Substring(start, end - start);
                            result.Add(next);
                            start = value.IndexOf("'", end + 1) + 1;
                        } while (start > 0);

                        if (result.Count() == 1) Settings[key] = result.Single(); //a list with one element is converted into a single string
                        else Settings[key] = result;

                    }
                    else if (int.TryParse(value, out int result))
                    {
                        Settings[key] = result;
                    }
                    else
                    {
                        try
                        {
                            Settings[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
                        }
                        catch (FormatException)
                        {
                            Trace.WriteLine($"Unrecognized key '{key}' in ConfigManager; it will be ignored.");
                        }
                    }

                }
                catch
                {
                    Trace.WriteLine($"Failed to read line in config file: {line}");
                }
                
            }
        }

        public static void WriteConfigFile(string path = "KeyTrain.cfg")
        {
            StreamWriter sw = new StreamWriter(path);

            foreach (var item in Settings.dicts.First())
            {
                var v = item.Value;
                
                if(v is IList)
                {
                    List<object> en = (v as IEnumerable<object>).Cast<object>().ToList();
                    sw.WriteLine($"{item.Key}: {string.Join(", ", en.Select(x => DynamicToString(x)))}" );
                }
                else
                {
                    sw.WriteLine($"{item.Key}: {DynamicToString(v)}" );
                }
               
                //Trace.WriteLine($"{item.Key}: {result}");
            }
            sw.Close();
        }

        static string DynamicToString(dynamic d)
        {
            if(d is string)
            {
                return $"'{d}'";
            }
            else
            {
                return d.ToString();
            }
        }
    }
}
