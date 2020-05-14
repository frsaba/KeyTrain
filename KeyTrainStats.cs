using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Markup;
using static Pythonic.ListHelpers;
using System.Windows.Media;
using System.Security.Policy;

namespace KeyTrainWPF
{
    static class KeyTrainStats
    {

        static DefaultDict<char, TimeData> charTimes = new DefaultDict<char, TimeData>();
        static DefaultDict<char, MissData> charMisses = new DefaultDict<char, MissData>();
        
        public static List<double> WPMLOG { get; private set; } =  new List<double>();
        public static double LastWPM => WPMLOG.LastOrDefault();

        public static List<int> MISSLOG { get; private set; } = new List<int>();
        public static int LastMissCount => MISSLOG.LastOrDefault();

        static List<(Color color, double delta)> RatingPalette = new List<(Color, double)>
        {
            (Colors.OrangeRed,      -150),
            (Colors.Orange,         -75),
            (Colors.YellowGreen,    -25),
            (Colors.LawnGreen,    50),    
            (Colors.Green,          100),
            (Colors.ForestGreen,    double.MaxValue)
        };

        public static void Enter(string text, TimeSpan[] times, SortedSet<int> misses, double? totalMinutes = null)
        {
            for (int i= 0; i < text.Length; i++)
            {
                char c = char.ToUpper(text[i]);
                charTimes[c].Add(i > 0 ? times[i] - times[i-1] : times[i] );
                charMisses[c].Log(misses.Contains(i));
                //charTimes[char.ToUpper(text[i])].Add(times[i] - times.ElementAtOrDefault(i - 1));
            }
            foreach (char k in charTimes.Keys)
            {
                charMisses[k].Flatten();
                Trace.WriteLine
                    ($"{k} - AVGSPEED: {charTimes[k].average:0.00} " +
                    $"MISSES: {string.Join(';', charMisses[k].values.Select(x => $"{x.missed}/{x.correct}").ToList())} ");
                    
                //Trace.WriteLine($"{k} - AVGSPEED: {charTimes[k].average:0.00} values: {string.Join(';', charTimes[k].values)} ");
            }

            totalMinutes ??= times.Sum(x => x.TotalMinutes);
            WPMLOG.Add(WPM(text.Length, (double)totalMinutes));
            MISSLOG.Add(misses.Count);
        }

        //TODO: property/expression body here?
        public static DefaultDict<char, Color> GetLetterRatings()
        {
            DefaultDict<char, Color> result = new DefaultDict<char, Color>();
            double avgAllLetters = LPM(WPMLOG.Average());
            foreach (char c in charTimes.Keys)
            {
                double avgThisLetter = charTimes[c].avgLPM;
                Trace.WriteLine($"{c}: {avgThisLetter}");

                double delta = avgThisLetter - avgAllLetters;
                for (int i = 0; i < RatingPalette.Count; i++)
                {
                    if(delta < RatingPalette[i].delta)
                    {
                        result[c] = RatingPalette[i].color;
                        break;
                    }
                }

                
            }
            return result;
        }

        public static double LPM(int length, double minutes) => length / minutes;
        public static double LPM(double WPM) => 5 * WPM;
        public static double WPM(int length, double minutes) => LPM(length, minutes) / 5;
    }

    
    class TimeData
    {
        public List<double> values { get; private set; }
        public double average { get; private set; }
        public double avgLPM => 60000 / average;

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

    class MissData
    {
        public List<(int missed, int correct)> values { get; private set; }
        public int totalMissed => values.Select(x => x.missed).Sum();
        public int total => values.Select(x => x.missed + x.correct).Sum();

        private int missAcc, correctAcc;

        public double average { get; private set; }
        public MissData()
        {
            values = new List<(int, int)>();
            average = 0;
            missAcc = 0; correctAcc = 0;
        }

        public void Add(int missed, int correct)
        {
            values.Add((missed, correct));
            average = totalMissed / total;
        }
        public void Log(bool miss)
        {
            if (miss == true) missAcc++; else correctAcc++;
        }

        public void Flatten()
        {
            Add(missAcc, correctAcc);
            missAcc = 0; correctAcc = 0;
        }
    }
}
