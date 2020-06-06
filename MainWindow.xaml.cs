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
using static KeyTrain.DarkStyles.MainWindow;
using Microsoft.Win32;
using System.IO;

namespace KeyTrainWPF
{
   
    public partial class MainWindow : Window
    {
        static string Text;
        //static LessonGenerator generator = new PresetTextLesson("Lorem ipsum dolor sit amet, " +
        //"consectetur adipiscing elit, sed do eiusmod tempor " +
        //"incididunt ut labore et dolore magna aliqua. ");


        //TODO: serialize dict file choice (general config file-class?)

        static ChainMap<string,dynamic> CFG = ConfigManager.Settings;

        static LessonGenerator generator;
        static HashSet<char> selectedChars = new HashSet<char>();
        SortedSet<int> misses = new SortedSet<int>();
        static Stopwatch timer;
        static KeyTrainStats stats = new KeyTrainStats();

        int ratingsDrawn = 0;
        static TimeSpan[] times;

        static class Pointer
        {
            public static int position = 0;
            public static char letter => Text[position];
            //public static char drawnLetter => Text[position]; 
        }
        
        class LetterRating : LetterRatingStyle
        {
           
            public static UniformGrid grid;
            public static MainWindow window;
            public static string toInclude = " .,;?!";
            Label l;
            char letter = '-';
            bool hasData = true;
            bool isSelected => selectedChars.Contains(letter);

            SolidColorBrush borderColor { get => isSelected ? highlightBorder : (hasData ? normalColor : inactiveColor); }
            public LetterRating(char letter, Color bgcolor, bool hasData = true)
            {
                this.letter = letter;
                this.hasData = hasData;
                l = LabelWithStyle(bgcolor, hasData);
                l.Content = letter.ToString();

                l.Foreground = hasData ? normalColor : inactiveColor;
                l.BorderBrush = borderColor;
                l.MouseEnter += (obj, mouseEvent) => { l.BorderBrush = highlightBorder; l.BorderThickness = new Thickness(3); };
                l.MouseLeave += (obj, mouseEvent) => { l.BorderBrush = borderColor; l.BorderThickness =  new Thickness(2); };
                l.MouseUp +=    (obj, mouseEvent) => {
                    _ = isSelected ? selectedChars.Remove(letter) : selectedChars.Add(letter);

                    if(generator.GetType() == typeof(RandomizedLesson)) {
                        ((RandomizedLesson)generator).Emphasize(selectedChars);
                        Text = generator.NextText();
                        window.Reset();
                    }
                };
                var ct = stats.charTimes[letter];
                var ms = stats.charMisses[letter];

                

                l.ToolTip = new ToolTip() {
                    Content = hasData ?
                       $"Avg. speed: {WPM_From_ms(ct.average):0.00} WPM\n" +
                       $"Last speed: {WPM_From_ms(ct.values.Last()):0.00} WPM\n" +
                       $"Error rate: {ms.errorRate:p}"
                       : "No data", 
                    FontFamily = new FontFamily("Courier")};
                ToolTipService.SetInitialShowDelay(l,stdTooltipDelay);
                
                grid.Children.Add(l);
            }
            public static void SetParent(UniformGrid grid, MainWindow window)
            {
                LetterRating.grid = grid;
                LetterRating.window = window;
            }
        }

        
        static Run typed  = RunWithStyle(typedStyle),
                mistakes  = RunWithStyle(mistakesStyle, text: wordJoiner),
                active    = RunWithStyle(activeStyle),
                remaining = RunWithStyle(remainingStyle);


        //TODO: move cursor blinking, at least move it outside the MainWindow constructor
        
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
            ConfigManager.ReadConfigFile();
            Focusable = true;
            generator = RandomizedLesson.FromDictionaryFiles(CFG["dictionaryPath"]);
            Text = generator.CurrentText;
            LetterRating.SetParent(letterRatings, this);
            cursorBlinker = new Timer((e) => {
                Dispatcher.Invoke(() =>
                {
                    if(Keyboard.FocusedElement == Main )
                    {
                        active.Background = new SolidColorBrush( blinkState ? cursorBgColors.Item1 : cursorBgColors.Item2);
                        active.Foreground = new SolidColorBrush( blinkState ? cursorFgColors.Item1 : cursorFgColors.Item2);
                    }
                    
                });
                blinkState = !blinkState;
            }, null, TimeSpan.Zero, blinkTime);
            
            stats = KeyTrainSerializer.Deserialize(ConfigManager.profilePath);
            ToolTipService.SetInitialShowDelay(HUD_WPM, stdTooltipDelay);
            ToolTipService.SetInitialShowDelay(HUD_misses, stdTooltipDelay);


            Reset();
            UpdateHUD();
        }

        // InlineCollection cannot be instantiated directly so we cannot just .prepend and set it. This is how we replace Main.Inlines with a new List<Inlines>
        void OverwriteMainIC(List<Inline> inlines)
        {
            Main.Inlines.Clear();
            foreach (var il in inlines)
            {
                Main.Inlines.Add(il);
            }
        }

        /// <summary>
        /// Formats the main textblock's inlines based on the current state. Mainly concerned with highlighting the errors in red
        /// </summary>
        void UpdateMain()
        {
            remaining.Text = Text.Substring(Pointer.position + 1);
            active.Text = Pointer.letter.ToString();// +  wordJoiner;
            var ic = Main.Inlines; 

            int mcount = misses.Count == 0 || misses.Last() != Pointer.position ? 
                misses.Count : misses.Count - 1;

            int typed_il_count = 2 * mcount + 1;


            while (ic.Count  < typed_il_count + 3)
            {
                var il = ConcatToList<Inline>(
                    RunWithStyle(typedStyle),
                    RunWithStyle(errorStyle),
                    Main.Inlines.ToList());
                OverwriteMainIC(il);
            }

            var mborders = new List<int>() {0, Pointer.position};
            mborders.InsertRange(1, misses.Take(mcount).SelectMany(
                x => new int[] { x, x + 1 } ));

            for (int i = 0; i < ic.Count - 3; i++)
            {
                Run r = (Run)ic.ElementAt(i);
 
                string t = Text.Substring(mborders[i], mborders[i+1] - mborders[i]);
                if (t.EndsWith(" ") || t.EndsWith(spaceReplacement) )
                {
                    t += ZWSP;
                }
                else
                {
                    t += wordJoiner;
                }
                r.Text = t;
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
                if(mistakes.Text.Length > wordJoiner.Length)
                {
                    mistakes.Text = mistakes.Text.Remove(mistakes.Text.Length - 1);
                    UpdateMain();
                }
                return;
            }

            //Trace.WriteLine(e.Text);
            char c;
            try { 
                c = e.Text[0];
                if (e.Text.First() == '\\') { return; }
            }
            catch { return; }

            //Debug.Content = $"Key: {c}, Correct: {Cursor.letter}";

            //Hide cursor
            Main.Cursor = Cursors.None;
            lastMousePos = Mouse.GetPosition(Wrapper);

            //Correct letter and no running mistakes
            if (c == Pointer.letter && mistakes.Text.Length == wordJoiner.Length)
            {
                timer.Stop();
                times[Pointer.position] = timer.Elapsed;
                ResetCursorBlink();
                Pointer.position++;
            }
            else //Miss
            {
                misses.Add(Pointer.position);      
                mistakes.Text += c;
            }

            timer.Start();
            if (Pointer.position == Text.Length)
            {
                NextText();
            }
            UpdateMain();

        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //Reset with Ctrl+R
            if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                Reset();
            }

            //Export with Ctrl+E 
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                KeyTrainSerializer.Serialize(stats, ConfigManager.profilePath);
                ConfigManager.WriteConfigFile();
            }
        }

        /// <summary>
        /// Advances to the next text chunk
        /// </summary>
        void NextText()
        {
            stats.Enter(Text, times, misses, timer.Elapsed.TotalMinutes);
            UpdateHUD();

            Text = generator.NextText();
            Reset();
        }
        void Reset(bool update = true)
        {
            typed.Text = ""; mistakes.Text = wordJoiner; active.Text = ""; remaining.Text = Text;
            misses.Clear();
            timer = new Stopwatch();
            times = new TimeSpan[Text.Length];
            Pointer.position = 0;
            List<Inline> inlines = new List<Inline>();
            inlines.AddRange(ConcatToList<Run>(typed, mistakes, active, remaining));

            OverwriteMainIC(inlines);
            Main.Focus(); //Clicking buttons focuses them which makes Main unresponsive, forever.
            if (update) UpdateMain();
        }

        void UpdateHUD()
        {
            double oldWPMAvg = stats.WPMLOG.DefaultIfEmpty(0).Average();
            double oldMissAvg = stats.MISSLOG.DefaultIfEmpty(0).Average();
            double wpm = stats.LastWPM;
            int misscount = stats.LastMissCount;
            wpmcounter.Text = $"{wpm:0.00}";
            misscounter.Text = $"{misscount:0}";
            var dict = new Dictionary<string,int>();
            dict.ToDefaultDict();
            HUD_WPM.ToolTip = new ToolTip()
            {
                Content = stats.WPMLOG.Count > 0 ? (
                $"Average: {stats.WPMLOG.DefaultIfEmpty(0).Average():0.##} WPM\n" +
                $"Fastest speed: {stats.WPMLOG.Max():0.##} WPM") : "No data"
            };
            HUD_misses.ToolTip = new ToolTip(){ 
                Content = $"Overall error rate: {(stats.charMisses.Count > 0 ? stats.charMisses.Average(x => x.Value.errorRate) : 0):p}\n" +
                $"Average misses per lesson: {(stats.MISSLOG.Count > 0 ? stats.MISSLOG.Average() : 0):0.##}" };
            ConditionalFormat(wpmgain, wpm - oldWPMAvg);
            ConditionalFormat(missgain, misscount - oldMissAvg, inverted: true);
            DrawLetterRatings();
            //RatingsChanged();
        }
        void DrawLetterRatings()
        {
            letterRatings.Children.Clear(); //TODO: overwrite existing instead
            DefaultDict<char,(Color color, bool active)> lrs = stats.GetLetterRatings(
                    alwaysInclude: generator.alphabet.Union(LetterRating.toInclude).ToHashSet());
            ratingsDrawn = lrs.Count;
            var keys = lrs.Keys.
                OrderBy(c => !lrs[c].active)
                .ThenBy(c => !char.IsLetterOrDigit(c))
                .ThenBy(c => c);

            foreach (char k in keys)
            {
                new LetterRating(k, lrs[k].color, lrs[k].active);
            }
            RatingsChanged();
        }


        private void dictFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Choose dictionary file(s)",
                Multiselect = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Resources"),
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };
           
            
            if(dialog.ShowDialog() == true)
            {
                ConfigManager.dictionaryPaths = dialog.FileNames.Select(p => new string[] 
                    {p, Path.GetRelativePath(Directory.GetCurrentDirectory(), p)}   //Compare absolute and relative paths
                    .OrderBy(p => p.Count(c => c == '\\')).First()).ToList();       //Keep the simpler one

                generator = RandomizedLesson.FromDictionaryFiles(ConfigManager.dictionaryPaths);
                Text = generator.NextText();
                Reset();
                UpdateHUD();
            }
            
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) 
        {
            if (e.WidthChanged)
            {
                RatingsChanged(windowWidth:e.NewSize.Width);
            }
            if (e.HeightChanged)
            {
                Main.Margin = new Thickness(15,
                    (e.NewSize.Height - HUD.Height - Main.ActualHeight) / 4 + HUD.Height, 15, 0);
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

        Point lastMousePos;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(Wrapper);

            if((lastMousePos - mousePos).Length > 2)
            {
                //Unhide cursor
                Main.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Sets the Main textblock's horizontal alignment for minimal jitter
        /// Single line -> center
        /// Multiple lines -> left
        /// </summary>
        private void SetMainAlignment(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                double singleLineHeight = Main.Padding.Top + Main.Padding.Bottom + Main.LineHeight;

                if (Main.ActualHeight > singleLineHeight)
                {
                    Main.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else
                {
                    Main.HorizontalAlignment = HorizontalAlignment.Center;
                }
            }
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            KeyTrainSerializer.Serialize(stats, CFG["profilePath"]);
            cursorBlinker.Dispose();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HUD.Opacity = 0.75;
            Main.Opacity = 0.25;
            timer.Stop(); //let's be nice and not count the time when we're tabbed away
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            HUD.Opacity = 1;
            Main.Opacity = 1;
        }


        
    }
}
