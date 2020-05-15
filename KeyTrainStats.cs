using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Markup;
using static Pythonic.ListHelpers;
using static KeyTrain.KeyTrainStatsConversion;
using System.Windows.Media;
using System.Security.Policy;

namespace KeyTrain
{   
    /// <summary>
    /// Class for legging and querying keyboard training statistics
    /// </summary>
    static class KeyTrainStats
    {

        public static DefaultDict<char, TimeData> charTimes { get; private set; } = new DefaultDict<char, TimeData>() ;
        public static DefaultDict<char, MissData> charMisses { get; private set; } = new DefaultDict<char, MissData>();
        
        public static List<double> WPMLOG { get; private set; } =  new List<double>();
        public static double LastWPM => WPMLOG.DefaultIfEmpty(0).Last();

        public static List<int> MISSLOG { get; private set; } = new List<int>();
        public static int LastMissCount => MISSLOG.LastOrDefault();

        public static List<(Color color, double delta)> RatingPalette { get; private set; } = new List<(Color, double)>
        {
            (Colors.OrangeRed,      -150),
            (Colors.Orange,         -75),
            (Colors.YellowGreen,    -25),
            (Colors.LawnGreen,      50),    
            (Colors.Green,          100),
            (Colors.ForestGreen,    double.MaxValue),
            //(Color.FromArgb(150,99,99,99),       double.PositiveInfinity) //invalid delta -> no data
            (Colors.Transparent,       double.PositiveInfinity) //invalid delta -> no data
        }; //TODO: move to DarkStyles

        /// <summary>
        /// Log the lesson's data to database
        /// </summary>
        /// <param name="text">The text that was used</param>
        /// <param name="times">The timestamps of each keypress</param>
        /// <param name="misses">Index of characters which very missed</param>
        /// <param name="totalMinutes">Time the lesson took in minutes. Calculated from times if left empty </param>
        public static void Enter(string text, TimeSpan[] times, SortedSet<int> misses, double? totalMinutes = null)
        {
            DefaultDict<char, (int misses, int total)> counts = new DefaultDict<char, (int, int)>();

            for (int i= 0; i < text.Length; i++)
            {
                char c = char.ToUpper(text[i]);
                charTimes[c].Add(i > 0 ? times[i] - times[i-1] : times[i] );
                counts[c] = (counts[c].misses + (misses.Contains(i) ? 1 : 0), counts[c].total + 1);
            }
            foreach (char k in charTimes.Keys)
            {
                charMisses[k].Add(counts[k].misses, counts[k].total);
                Trace.WriteLine(
                    $"{k} - AVGSPEED: {charTimes[k].average:0.00} " +
                    $"MISSES: {string.Join(';', charMisses[k].values.Select(x => $"{x.missed}/{x.total}").ToList())} ");
                    
                //Trace.WriteLine($"{k} - AVGSPEED: {charTimes[k].average:0.00} values: {string.Join(';', charTimes[k].values)} ");
            }

            totalMinutes ??= times.Last().TotalMinutes - times.First().TotalMinutes;
            WPMLOG.Add(WPM(text.Length, (double)totalMinutes));
            MISSLOG.Add(misses.Count);
        }

        /// <summary>
        /// Compares each letter's average to the overall letter average and rates them via colors defined by RatingPalette
        /// </summary>
        /// <param name="alwaysInclude">Include these characters even if there's no data about them.</param>
        /// <returns>For each letter: Tuple of rating (color) and whether there was data available (hadData bool) </returns>
        public static DefaultDict<char, (Color color, bool hadData)> GetLetterRatings(HashSet<char> alwaysInclude = null)
        {
            alwaysInclude ??= new HashSet<char>();

            DefaultDict<char, (Color, bool)> result = new DefaultDict<char, (Color, bool)>();
            double avgAllLetters = LPM_From_WPM(WPMLOG.DefaultIfEmpty(0).Average());
            foreach (char c in charTimes.Keys.Union(alwaysInclude))
            {
                double avgThisLetter = charTimes[c].avgLPM;

                double delta = avgThisLetter - avgAllLetters;
                bool hadData = double.IsFinite(avgThisLetter);
                for (int i = 0; i < RatingPalette.Count; i++)
                {
                    if(delta <= RatingPalette[i].delta)
                    {
                        result[c] = (RatingPalette[i].color, hadData);
                        break;
                    }
                }

                //Trace.WriteLine($"{c}: {avgThisLetter}");
            }
            return result;
        }

        
    }

    static class KeyTrainStatsConversion
    {
        public static double LPM(int length, double minutes) => length / minutes;
        public static double LPM_From_WPM(double WPM) => 5 * WPM;
        public static double WPM_From_ms(double milliseconds) => 12000 / milliseconds;

        public static double WPM(int length, double minutes) => LPM(length, minutes) / 5;
    }

    
    class TimeData
    {
        public List<double> values { get; private set; }
        public double average { get; private set; }
        public double avgLPM => 60000 / average; //returns infinity when there's no data

        public TimeData()
        {
            values = new List<double>();
            average = 0;
        }

        public void Add(TimeSpan s)
        {
            Add(s.TotalMilliseconds);
        }
        private void Add(double d)
        {
            d = Math.Round(d);
            if(d != 0)
            {
                values.Add(d);
                //average = ((average * values.Count - 1) + d) / values.Count;
                average = values.Average();
            }
        }


    }

    //Counts are logged separately for each text in order to facilitate by-time visualization
    class MissData
    {
        public List<(int missed, int total)> values { get; private set; }
        public int totalMissed => values.Select(x => x.missed).Sum();
        public int total => values.Select(x => x.total).Sum();

        public double missratio { get; private set; }
        public MissData()
        {
            values = new List<(int, int)>();
            missratio = 0;
        }

        public void Add(int missed, int outOf)
        {
            values.Add((missed, outOf));
            missratio = total > 0 ? (double)totalMissed / total : 0;
        }
    }
}
