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
using static KeyTrainWPF.KeyTrainStats;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Dynamic;
using System.Threading;
using System.ComponentModel;

namespace WpfApp1
{
   
    public partial class MainWindow : Window
    {
        static string Text;
        //LessonGenerator generator = new PresetTextLesson("Lorem ipsum dolor sit amet, " +
        //    "consectetur adipiscing elit, sed do eiusmod tempor " +
        //    "incididunt ut labore et dolore magna aliqua. ");

        static LessonGenerator generator = new RandomizedLesson();
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
        static HashSet<char> selectedChars = new HashSet<char>();


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
            public const int width = 45;
            public const int height = 45;
            static UniformGrid grid;
            static MainWindow window;
            Label l = new Label();
            char letter = '-';
            static SolidColorBrush normalBorder = new SolidColorBrush(Colors.Black);
            static SolidColorBrush highlightBorder = new SolidColorBrush(Colors.White);
            SolidColorBrush borderColor { get => selectedChars.Contains(letter) ? highlightBorder : normalBorder; }
            public LetterRating(char letter, Color bgcolor)
            {
                this.letter = letter;
                l.Content = letter.ToString();
                l.Width = width;
                l.Height = height;
                
                l.HorizontalContentAlignment = HorizontalAlignment.Center;
                l.VerticalContentAlignment = VerticalAlignment.Center;
                //l.Padding = new Thickness(5);
                l.FontSize = 30;
                l.FontWeight = FontWeights.DemiBold;
                l.Background = new SolidColorBrush(bgcolor);
                l.Foreground = new SolidColorBrush(Colors.Black);
                l.FontWeight = FontWeights.Normal;

                l.BorderBrush = borderColor;
                l.BorderThickness = new Thickness(1);

                l.MouseEnter += (obj, mouseEvent) => { l.BorderBrush = highlightBorder; l.BorderThickness = new Thickness(2); };
                l.MouseLeave += (obj, mouseEvent) => { l.BorderBrush = borderColor; l.BorderThickness =  new Thickness(1); };
                l.MouseUp +=    (obj, mouseEvent) => {
                    _ = selectedChars.Contains(letter) ? selectedChars.Remove(letter) : selectedChars.Add(letter);

                    if(generator.GetType() == typeof(RandomizedLesson)) {
                        ((RandomizedLesson)generator).Emphasize(selectedChars);
                        Text = generator.NextText();
                        window.Reset();
                        
                    }
                };
                ToolTip t = new ToolTip();
                if (charTimes[letter].values.Count > 0)
                {
                    t.Content = $"Average speed: {WPM_From_ms(charTimes[letter].average):0.00} WPM\n" +
                        $"Last speed: {WPM_From_ms(charTimes[letter].values.Last()):0.00} WPM";
                }
                else
                {
                    t.Content = "No data";
                }

                l.ToolTip = t;
                ToolTipService.SetInitialShowDelay(l,750);
                grid.Children.Add(l);
            }
            public static void SetParent(UniformGrid grid, MainWindow window)
            {
                LetterRating.grid = grid;
                LetterRating.window = window;
            }

        }

        static Run RunWithStyle(SectionStyle style = null, string text = "")
        {
            style ??= new SectionStyle();
            Run r = new Run(text);
            r.Foreground = new SolidColorBrush(style.fg);
            r.Background = new SolidColorBrush(style.bg);
            return r;
        }

        
        static SectionStyle missedStyle = new SectionStyle(fgColor: Color.FromRgb(201, 23, 10));
        static SectionStyle typedstyle = new SectionStyle(fgColor: Colors.Gray);

        static Run typed  = RunWithStyle(typedstyle),
                mistakes  = RunWithStyle(new SectionStyle(bgColor: Color.FromRgb(201, 23, 10))),
                active    = RunWithStyle(new SectionStyle(fgColor: Colors.Black, bgColor: Colors.Silver)),
                remaining = RunWithStyle(new SectionStyle());
       

        static (Color, Color) activeBgColors = (Colors.Silver, wrapperBackground);
        static (Color, Color) activeFgColors = (Colors.Black, Colors.White);
        static TimeSpan blinkTime = TimeSpan.FromMilliseconds(600);
        Timer cursorBlinker;
        bool blinkState = true;
        void ResetCursorBlink()
        {
            blinkState = true;
            cursorBlinker.Change(TimeSpan.Zero, blinkTime);
        }
        
        
        public MainWindow()
        {
            InitializeComponent();
            Text = generator.CurrentText;
            LetterRating.SetParent(letterRatings, this);
            cursorBlinker = new Timer((e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if(Keyboard.FocusedElement == this)
                    {
                        active.Background = new SolidColorBrush( blinkState ? activeBgColors.Item1 : activeBgColors.Item2);
                        active.Foreground = new SolidColorBrush( blinkState ? activeFgColors.Item1 : activeFgColors.Item2);
                    }
                    
                });
                blinkState = !blinkState;
            }, null, TimeSpan.Zero, blinkTime);
            //new LetterRating('T', Colors.DarkGreen);
            Reset();
            UpdateHUD();
        }

        void OverwriteMainIC(List<Inline> inlines)
        {
            Main.Inlines.Clear();
            foreach (var il in inlines)
            {
                Main.Inlines.Add(il);
            }
        }

        SortedSet<int> misses = new SortedSet<int>();      

        //TODO: manual overflow. builtin often linebreaks on inline borders which is distracting
        void UpdateMain()
        {
            remaining.Text = Text.Substring(Cursor.position + 1);
            active.Text = Cursor.drawnLetter.ToString();
            Run[] p = new Run[] {mistakes, active, remaining };
            var ic = Main.Inlines;

            int mcount = misses.Count == 0 || misses.Last() != Cursor.position ? 
                misses.Count : misses.Count - 1;

            int typed_il_count = 2 * mcount + 1;

            while (ic.Count  < typed_il_count + 3)
            {
                var il = ConcatToList<Inline>(
                    RunWithStyle(new SectionStyle(fgColor: Colors.Gray)), //TODO: replace with typedstyle
                    RunWithStyle(missedStyle),
                    Main.Inlines.ToList());
                OverwriteMainIC(il);
            }

            var mborders = new List<int>() {0, Cursor.position};
            mborders.InsertRange(1, misses.Take(mcount).SelectMany(
                x => new int[] { x, x + 1 } ));

            for (int i = 0; i < ic.Count - 3; i++)
            {
                Run r = (Run)ic.ElementAt(i);
 
                r.Text = Text.Substring(mborders[i], mborders[i+1] - mborders[i]);
                if (i % 2 == 1) //Are we drawing an error?
                    r.Text = r.Text.Replace(" ", spaceReplacement);
            }
        }

        //TODO: ignore newline/control characters
        private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            //Backspace
            if (e.Text == "\b" )
            {
                if(string.IsNullOrEmpty(mistakes.Text) == false)
                {
                    mistakes.Text = mistakes.Text.Remove(mistakes.Text.Length - 1);
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
            if (c == Cursor.letter && string.IsNullOrEmpty(mistakes.Text))
            {
                typed.Text += c;
                timer.Stop();
                times[Cursor.position] = timer.Elapsed;
                ResetCursorBlink();
                Cursor.position++;
            }
            else //Miss
            {
                misses.Add(Cursor.position);      
                mistakes.Text += c;
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
            (windowWidth - margins) / LetterRating.width,
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
            KeyTrainStats.Enter(Text, times, misses, timer.Elapsed.TotalMinutes);
            UpdateHUD();

            Text = generator.NextText();
            Reset();
        }
        
        void UpdateHUD()
        {
            double oldWPMAvg = KeyTrainStats.WPMLOG.DefaultIfEmpty(0).Average();
            double oldMissAvg = KeyTrainStats.MISSLOG.DefaultIfEmpty(0).Average();
            double wpm = KeyTrainStats.LastWPM;
            int misscount = KeyTrainStats.LastMissCount;
            wpmcounter.Text = $"{wpm:0.00}";
            misscounter.Text = $"{misscount:0}";
            ConditionalFormat(wpmgain, wpm - oldWPMAvg);
            ConditionalFormat(missgain, misscount - oldMissAvg, inverted: true);
            DrawLetterRatings();
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
            DefaultDict<char,Color> lrs = KeyTrainStats.GetLetterRatings(generator.alphabet);
            ratingsDrawn = lrs.Count;
            foreach (char key in lrs.Keys.OrderBy(c => lrs[c] == RatingPalette.Last().color).ThenBy(c => c))
            {
                new LetterRating(key, lrs[key]);
            }
            RatingsChanged(Width);
        }

        void Reset(bool update = true)
        {
            typed.Text = ""; mistakes.Text = ""; active.Text = ""; remaining.Text = Text;
            misses.Clear();
            timer = new Stopwatch();
            times = new TimeSpan[Text.Length];
            Cursor.position = 0;
            List<Inline> inlines = new List<Inline>();
            inlines.AddRange(ConcatToList<Run>(typed, mistakes, active, remaining));

            OverwriteMainIC(inlines);
            if(update) UpdateMain();
        }

        
    }
}
