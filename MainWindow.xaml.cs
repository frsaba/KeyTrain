using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using KeyTrainWPF;
using Pythonic;
using static Pythonic.ListHelpers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Dynamic;
using System.Threading;

namespace WpfApp1
{
   
    public partial class MainWindow : Window
    {
        static string Text;
        //LessonGenerator generator = new PresetTextLesson("Lorem ipsum dolor sit amet, " +
        //    "consectetur adipiscing elit, sed do eiusmod tempor " +
        //    "incididunt ut labore et dolore magna aliqua. ");

        LessonGenerator generator = new RandomizedLesson();
        static Stopwatch timer;
        const string spaceReplacement = "·"; //could be "␣" but it takes up 2 spaces which is a weird look)
        int ratingsDrawn = 0;
        static TimeSpan[] times;

        static Color wrapperBackground = (Color)ColorConverter.ConvertFromString("#1f1f1f");
        static List<(Color color, double delta)> gainPalette = new List<(Color, double)>
        {
            (Colors.Tomato,      -0.5),
            (Colors.Silver,         0.2),
            (Colors.LightGreen,    double.MaxValue)
        };

        new static class Cursor
        {
            public static int position = 0;
            public static char letter { get => Text[position]; }
            public static char drawnLetter { get => Text[position]; }
        }
        class SectionStyle
        {
            public string startText;
            public Color fg;
            public Color bg;

            public SectionStyle(string text = "", Color? fgColor = null, Color? bgColor = null)
            {
                startText = text;
                //TODO: inherit from wrapper
                fg = fgColor ?? Colors.White;
                bg = bgColor ?? wrapperBackground;
            }
        }
        class LetterRating
        {
            public const int width = 35;
            static UniformGrid grid;
            Label l = new Label();
            //Border border = new Border();
            public LetterRating(char letter, Color bgcolor)
            {
                l.Content = letter.ToString();
                l.Width = width;

                l.HorizontalAlignment = HorizontalAlignment.Center;
                l.HorizontalContentAlignment = HorizontalAlignment.Center;
                l.VerticalAlignment = VerticalAlignment.Center;
                l.Padding = new Thickness(5, 3, 5, 0);
                l.FontSize = 25;
                l.Background = new SolidColorBrush(bgcolor);
                l.Foreground = new SolidColorBrush(Colors.Black);
                l.FontWeight = FontWeights.Normal;
                
                l.BorderBrush = new SolidColorBrush(Colors.Black);
                l.BorderThickness = new Thickness(1);

                grid.Children.Add(l);
            }
            public static void SetParent(UniformGrid grid)
            {
                LetterRating.grid = grid;
            }

        }

        Run RunWithStyle(SectionStyle style = null, string text = "")
        {
            style ??= new SectionStyle();
            Run r = new Run(text);
            r.Foreground = new SolidColorBrush(style.fg);
            r.Background = new SolidColorBrush(style.bg);
            return r;
        }

        List<SectionStyle> sections = new List<SectionStyle>
        {   // typed
            new SectionStyle(fgColor:Colors.Gray),
            //missed
            new SectionStyle(bgColor:Color.FromRgb(201, 23, 10)),
            //active
            new SectionStyle(fgColor:Colors.Black, bgColor:Colors.Silver), 
            //remaining
            new SectionStyle()

        };

        //TODO: move this to a different class?
        static (SolidColorBrush, SolidColorBrush) activeBgColors = 
            (new SolidColorBrush( Colors.Silver), new SolidColorBrush(wrapperBackground));
        static (SolidColorBrush, SolidColorBrush) activeFgColors = 
            (new SolidColorBrush( Colors.Black), new SolidColorBrush(Colors.White));
        static TimeSpan blinkTime = TimeSpan.FromMilliseconds(600);
        Timer cursorBlinker;
        bool blinkState = true;
        void ResetCursorBlink()
        {
            blinkState = true;
            cursorBlinker.Change(TimeSpan.Zero, blinkTime);
        }
        
        SectionStyle missedStyle = new SectionStyle(fgColor: Color.FromRgb(201, 23, 10));
        
        public MainWindow()
        {
            InitializeComponent();
            Text = generator.CurrentText;
            LetterRating.SetParent(letterRatings);
            cursorBlinker = new Timer((e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if(Keyboard.FocusedElement == this)
                    {
                        var ic = Main.Inlines;
                        Run r = (Run)ic.ElementAt(ic.Count - 2);
                        r.Background = blinkState ? activeBgColors.Item1 : activeBgColors.Item2;
                        r.Foreground = blinkState ? activeFgColors.Item1 : activeFgColors.Item2;
                    }
                    
                });
                blinkState = !blinkState;
            }, null, TimeSpan.Zero, blinkTime);
            Reset();
        }

        void OverwriteMainIC(List<Inline> inlines)
        {
            Main.Inlines.Clear();
            foreach (var il in inlines)
            {
                Main.Inlines.Add(il);
            }
        }

        static string typed, mistakes, active, remaining = Text;
        SortedSet<int> misses = new SortedSet<int>();      

        //TODO: manual overflow. builtin often linebreaks on inline borders which is distracting
        void UpdateMain()
        {
            remaining = Text.Substring(Cursor.position + 1);
            active = Cursor.drawnLetter.ToString();
            string[] p = new string[] {mistakes, active, remaining };
            var ic = Main.Inlines;

            int mcount = misses.Count == 0 || misses.Last() != Cursor.position ? 
                misses.Count : misses.Count - 1;

            int typed_il_count = 2 * mcount + 1;

            while (ic.Count  < typed_il_count + 3)
            {
                var il = ConcatToList<Inline>(
                    RunWithStyle(sections[0]),
                    RunWithStyle(missedStyle),
                    Main.Inlines.ToList());
                OverwriteMainIC(il);
            }

            var mborders = new List<int>() {0, Cursor.position};
            mborders.InsertRange(1, misses.Take(mcount).SelectMany(
                x => new int[] { x, x + 1 } ));

            for (int i = 0; i < ic.Count; i++)
            {
                Run r = (Run)ic.ElementAt(i);
                //active, mistakes, remaining
                if (i >= ic.Count - 3)
                {
                    r.Text = p[i - typed_il_count];
                }
                //typed
                else
                {
                    r.Text = Text.Substring(mborders[i], mborders[i+1] - mborders[i]);
                    if (i % 2 == 1) //Are we drawing an error?
                        r.Text = r.Text.Replace(" ", spaceReplacement);
                }
            }
        }

        //TODO: ignore newline/control characters
        private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            //Backspace
            if (e.Text == "\b" )
            {
                if(string.IsNullOrEmpty(mistakes) == false)
                {
                    mistakes = mistakes.Remove(mistakes.Length - 1);
                    UpdateMain();
                }
                return;
            } 

            char c;
            try { 
                c = e.Text[0];
                if (e.Text.First() == '\\') { return; }
            }
            catch { return; }
            

            Debug.Content = $"Key: {c}, Correct: {Cursor.letter}";

            //Correct letter
            if (c == Cursor.letter && string.IsNullOrEmpty(mistakes))
            {
                typed += c;
                timer.Stop();
                times[Cursor.position] = timer.Elapsed;
                ResetCursorBlink();
                Cursor.position++;
            }
            else //Miss
            {
                misses.Add(Cursor.position);      
                mistakes += c;
            }

            timer.Start();
            if (Cursor.position == Text.Length)
            {
                NextText();
            }
            UpdateMain();

        }

        //This causes massive jump in memory usage in debug mode for some reason
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) 
        {
            if (e.WidthChanged)
            {
                RatingsChanged(windowWidth:e.NewSize.Width);
            }
            if (e.HeightChanged)
            {
                Main.Margin = new Thickness(0,
                    (e.NewSize.Height - HUD.Height - Main.Height - Debug.ActualHeight) / 4 + HUD.Height, 0, 0);
            }
        }
        private void RatingsChanged(double windowWidth)
        {
            double margins = letterRatings.Margin.Left + letterRatings.Margin.Right;
            letterRatings.Columns = (int)Math.Min(
            ((windowWidth - margins) / LetterRating.width),
                ratingsDrawn);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                Reset();
            }
        }

        void NextText()
        {
            double oldWPMAvg = KeyTrainStats.WPMLOG.DefaultIfEmpty(0).Average();
            double oldMissAvg = KeyTrainStats.MISSLOG.DefaultIfEmpty(0).Average();
            KeyTrainStats.Enter(Text, times, misses, timer.Elapsed.TotalMinutes);
            double wpm = KeyTrainStats.LastWPM;
            int misscount = KeyTrainStats.LastMissCount;
            wpmcounter.Text =   $"{wpm:0.00}";
            misscounter.Text =  $"{misscount:0}";
            ConditionalFormat(wpmgain, wpm - oldWPMAvg);
            ConditionalFormat(missgain, misscount - oldMissAvg, inverted:true);
            DrawLetterRatings();

            Text = generator.NextText();
            Reset();
        }
        
        void ConditionalFormat(Run run, double value, bool inverted = false)
        {
            run.Text = $"{value:+0.00;-0.00;0}";
            for (int i = 0; i < gainPalette.Count; i++)
            {
                if ((inverted ? -value : value) < gainPalette[i].delta)
                {
                    run.Foreground = new SolidColorBrush(gainPalette[i].color);
                    break;
                }
            }
        }

        void DrawLetterRatings()
        {
            letterRatings.Children.Clear(); //TODO: overwrite existing instead
            DefaultDict<char,Color> lrs = KeyTrainStats.GetLetterRatings();
            ratingsDrawn = lrs.Count;
            foreach (char key in lrs.Keys.OrderBy(x => x))
            {
                new LetterRating(key, lrs[key]);
            }
            RatingsChanged(Width);
        }

        void Reset(bool update = true)
        {
            typed = ""; mistakes = ""; active = ""; remaining = Text;
            misses.Clear();
            timer = new Stopwatch();
            times = new TimeSpan[Text.Length];
            //Debug.Content += "times: " + Text.Length;
            Cursor.position = 0;
            List<Inline> inlines = new List<Inline>();
            foreach (var s in sections)
            {
                inlines.Add(RunWithStyle(s, s.startText));
            }
            OverwriteMainIC(inlines);
            if(update) UpdateMain();
        }

        
    }
}
