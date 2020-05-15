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
using static KeyTrainWPF.KeyTrainStatsConversion;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Dynamic;
using System.Threading;
using System.ComponentModel;
using static KeyTrainWPF.DarkStyles.MainWindow;

namespace WpfApp1
{
   
    public partial class MainWindow : Window
    {
        static string Text;
        //static LessonGenerator generator = new PresetTextLesson("Lorem ipsum dolor sit amet, " +
        //    "consectetur adipiscing elit, sed do eiusmod tempor " +
        //    "incididunt ut labore et dolore magna aliqua. ");

        static LessonGenerator generator = new RandomizedLesson();
        static HashSet<char> selectedChars = new HashSet<char>();
        SortedSet<int> misses = new SortedSet<int>();
        static Stopwatch timer;
        
        int ratingsDrawn = 0;
        static TimeSpan[] times;

        new static class Cursor
        {
            public static int position = 0;
            public static char letter { get => Text[position]; }
            public static char drawnLetter { get => Text[position]; }
        }
        
        class LetterRating : LetterRatingStyle
        {
           
            public static UniformGrid grid;
            public static MainWindow window;
            Label l = new Label();
            char letter = '-';
            bool hasData = true;
            bool isSelected => selectedChars.Contains(letter);

            SolidColorBrush borderColor { get => isSelected ? highlightBorder : (hasData ? normalColor : inactiveColor); }
            public LetterRating(char letter, Color bgcolor, bool hasData = true)
            {
                this.letter = letter;
                this.hasData = hasData;
                l.Content = letter.ToString();
                SetLabelStyle(ref l,bgcolor, hasData);

                l.Foreground = hasData ? normalColor : inactiveColor;
                l.BorderBrush = borderColor;
                l.MouseEnter += (obj, mouseEvent) => { l.BorderBrush = highlightBorder; l.BorderThickness = new Thickness(2); };
                l.MouseLeave += (obj, mouseEvent) => { l.BorderBrush = borderColor; l.BorderThickness =  new Thickness(1); };
                l.MouseUp +=    (obj, mouseEvent) => {
                    _ = isSelected ? selectedChars.Remove(letter) : selectedChars.Add(letter);

                    if(generator.GetType() == typeof(RandomizedLesson)) {
                        ((RandomizedLesson)generator).Emphasize(selectedChars);
                        Text = generator.NextText();
                        window.Reset();
                        
                    }
                };
                ToolTip t = new ToolTip();
                var ct = KeyTrainStats.charTimes[letter];
                var ms = KeyTrainStats.charMisses[letter];
                
                if (ct.values.Count > 0) //if(active) might do the same thing
                {
                    t.Content = $"Avg. speed: {WPM_From_ms(ct.average):0.00} WPM\n" +
                                $"Last speed: {WPM_From_ms(ct.values.Last()):0.00} WPM\n" +
                                $"Miss ratio: {ms.missratio * 100:0.##}%";
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

        static Run typed  = RunWithStyle(typedstyle),
                mistakes  = RunWithStyle(new SectionStyle(bgColor: Color.FromRgb(201, 23, 10))),
                active    = RunWithStyle(new SectionStyle(fgColor: Colors.Black, bgColor: Colors.Silver)),
                remaining = RunWithStyle(new SectionStyle());
       


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
            

            //Debug.Content = $"Key: {c}, Correct: {Cursor.letter}";

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
                    (e.NewSize.Height - HUD.Height - Main.Height) / 4 + HUD.Height, 0, 0);
            }
        }
        private void RatingsChanged(double windowWidth = 0, double spacing = 2)
        {
            if (windowWidth == 0)
            {
                MainWindow w = LetterRating.window;
                windowWidth = Math.Max(w.Width, w.ActualWidth);
            }

            double margins = letterRatings.Margin.Left + letterRatings.Margin.Right;
            letterRatings.Columns = (int)Math.Min(
            (windowWidth - margins) / (LetterRating.width + spacing),
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
            RatingsChanged();
        }

        void DrawLetterRatings()
        {
            letterRatings.Children.Clear(); //TODO: overwrite existing instead
            DefaultDict<char,(Color color, bool active)> lrs = 
                KeyTrainStats.GetLetterRatings(alwaysInclude:generator.alphabet);
            ratingsDrawn = lrs.Count;
            foreach (char key in lrs.Keys.
                OrderBy(c => !lrs[c].active)
                .ThenBy(c => !char.IsLetterOrDigit(c))
                .ThenBy(c => c))
            {
                new LetterRating(key, lrs[key].color, lrs[key].active);
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
