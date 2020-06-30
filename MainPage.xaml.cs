using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using Pythonic;
using static Pythonic.ListHelpers;
using static KeyTrain.KeyTrainStatsConversion;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.ComponentModel;
using static KeyTrain.DarkStyles.MainPage;
using KeyTrainWPF;

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public static string Text;
        static string displayText(string t) => t.Replace(" ", $"{ZWSP} ");

        static ChainMap<string, dynamic> CFG = ConfigManager.Settings;

        public static LessonGenerator Generator;
        public static string selectedChars { get => CFG["emphasizedLetters"]; set => CFG["emphasizedLetters"] = value; }
        
SortedSet<int> misses = new SortedSet<int>();
        static Stopwatch timer;
        static KeyTrainStats stats = new KeyTrainStats();

        int ratingsDrawn = 0;
        static TimeSpan[] times;

        static class Pointer
        {
            public static int position = 0;
            public static char letter => Text[position];
            public static string displayLetter => displayText(letter.ToString());
        }

        class LetterRating : LetterRatingStyle
        {

            public static UniformGrid grid;
            public static MainPage page;
            public static string toInclude = " .,;?!-";
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
                l.MouseLeave += (obj, mouseEvent) => { l.BorderBrush = borderColor; l.BorderThickness = new Thickness(2); };
                l.MouseUp += (obj, mouseEvent) => {
                    selectedChars = isSelected ? selectedChars.Replace(letter.ToString(), "") : selectedChars + letter;
                    

                    if (Generator.GetType() == typeof(RandomizedLesson))
                    {
                        ((RandomizedLesson)Generator).Emphasize(selectedChars);
                        Text = Generator.NextText();
                        page.Reset();
                    }
                };
                var ct = stats.charTimes[letter];
                var ms = stats.charMisses[letter];



                l.ToolTip = new ToolTip()
                {
                    Content = hasData ?
                       $"Avg. speed: {WPM_From_ms(ct.average):0.00} WPM\n" +
                       $"Last speed: {WPM_From_ms(ct.values.Last()):0.00} WPM\n" +
                       $"Error rate: {ms.errorRate:p}"
                       : "No data",
                    FontFamily = new FontFamily("Courier")
                };
                ToolTipService.SetInitialShowDelay(l, stdTooltipDelay);

                grid.Children.Add(l);
            }
            public static void SetParent(UniformGrid grid, MainPage page)
            {
                LetterRating.grid = grid;
                LetterRating.page = page;
            }
        }


        static Run typed = RunWithStyle(typedStyle),
                mistakes = RunWithStyle(mistakesStyle, text: wordJoiner),
                active = RunWithStyle(activeStyle),
                remaining = RunWithStyle(remainingStyle);


        //TODO: move cursor blinking, at least move it outside the constructor

        Timer cursorBlinker;
        bool blinkState = true;
        void ResetCursorBlink()
        {
            blinkState = true;
            cursorBlinker.Change(TimeSpan.Zero, blinkTime);
        }

        public MainPage()
        {
            InitializeComponent();
            ConfigManager.ReadConfigFile();
            Focusable = true;
            Generator = RandomizedLesson.FromDictionaryFiles(CFG["dictionaryPath"]);
            Text = Generator.CurrentText;
            LetterRating.SetParent(letterRatings, this);
            cursorBlinker = new Timer((e) => {
                Dispatcher.Invoke(() =>
                {
                    if (Keyboard.FocusedElement == Main)
                    {
                        active.Background = new SolidColorBrush(blinkState ? cursorBgColors.Item1 : cursorBgColors.Item2);
                        active.Foreground = new SolidColorBrush(blinkState ? cursorFgColors.Item1 : cursorFgColors.Item2);
                    }

                });
                blinkState = !blinkState;
            }, null, TimeSpan.Zero, blinkTime);

            stats = KeyTrainSerializer.Deserialize(ConfigManager.profilePath);
            ToolTipService.SetInitialShowDelay(HUD_WPM, stdTooltipDelay);
            ToolTipService.SetInitialShowDelay(HUD_misses, stdTooltipDelay);
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
            remaining.Text = displayText(Text.Substring(Pointer.position + 1)); //ZWSP is not zero width, according to WPF.
            //If we add it afterwards, it takes up additional space which messes up line breaks
            active.Text = Pointer.displayLetter.ToString();// +  wordJoiner;
            var ic = Main.Inlines;

            int mcount = misses.Count == 0 || misses.Last() != Pointer.position ?
                misses.Count : misses.Count - 1;

            int typed_il_count = 2 * mcount + 1;


            while (ic.Count < typed_il_count + 3)
            {
                var il = ConcatToList<Inline>(
                    RunWithStyle(typedStyle),
                    RunWithStyle(errorStyle),
                    Main.Inlines.ToList());
                OverwriteMainIC(il);
            }

            var mborders = new List<int>() { 0, Pointer.position };
            mborders.InsertRange(1, misses.Take(mcount).SelectMany(
                x => new int[] { x, x + 1 }));

            for (int i = 0; i < ic.Count - 3; i++)
            {
                Run r = (Run)ic.ElementAt(i);

                string t = displayText(Text.Substring(mborders[i], mborders[i + 1] - mborders[i]));
                if (t.EndsWith(" ") || t.EndsWith(spaceReplacement))
                {
                    //t += ZWSP;
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
            if (e.Text == "\b")
            {
                if (mistakes.Text.Length > wordJoiner.Length)
                {
                    mistakes.Text = mistakes.Text.Remove(mistakes.Text.Length - 1);
                    UpdateMain();
                }
                e.Handled = true;
                return;
            }

            //Trace.WriteLine(e.Text);
            char c;
            try
            {
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
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                RestartButton_Click(sender, e);
                e.Handled = true;
            }
            //Reroll with Ctrl+N
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                RerollButton_Click(sender, e);
                e.Handled = true;
            }

            //Export with Ctrl+E 
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                KeyTrainSerializer.Serialize(stats, ConfigManager.profilePath);
                ConfigManager.WriteConfigFile();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Advances to the next text chunk and logs the data to KeyTrainStats
        /// </summary>
        public void NextText()
        {
            stats.Enter(Text, times, misses, timer.Elapsed.TotalMinutes);
            UpdateHUD();

            Text = Generator.NextText();
            Reset();
        }
        public void Reset(bool update = true)
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

        public void UpdateHUD()
        {
            double oldWPMAvg = stats.WPMLOG.DefaultIfEmpty(0).Average();
            double errorRate() => stats.charMisses.Count > 0 ? stats.charMisses.DefaultIfEmpty().Average(x => x.Value.errorRate) : 0;
            double errorsPerLength() => errorRate() * ConfigManager.lessonLength;
            //double oldMissAvg = stats.MISSLOG.DefaultIfEmpty(0).Average();
            double wpm = stats.LastWPM;
            int misscount = stats.LastMissCount;
            wpmcounter.Text = $"{wpm:0.00}";
            misscounter.Text = $"{misscount:0}";
            misscounter_annotation.Text = misscount == 1 ? "    miss" : "misses"; //spaces to keep the number in the the same place. \t made the text disappear, don't know why
            //var dict = new Dictionary<string, int>();
            //dict.ToDefaultDict();
            HUD_WPM.ToolTip = new ToolTip()
            {
                Content = stats.WPMLOG.Count > 0 ? (
                $"Average: {stats.WPMLOG.DefaultIfEmpty(0).Average():0.##} WPM\n" +
                $"Fastest speed: {stats.WPMLOG.Max():0.##} WPM") : "No data"
            };
            HUD_misses.ToolTip = new ToolTip()
            {
                Content = stats.charMisses.Count > 0 ? 
                ( $"Overall error rate: {errorRate():p}\n" +
                $"Average misses per lesson length: {errorsPerLength():0.##}") 
                : "No data"
            };
            ConditionalFormat(wpmgain, wpm - oldWPMAvg);
            ConditionalFormat(missgain, misscount - errorsPerLength(), inverted: true);
            DrawLetterRatings();
            //RatingsChanged();
        }
        void DrawLetterRatings()
        {
            letterRatings.Children.Clear(); //TODO: overwrite existing instead
            DefaultDict<char, (Color color, bool active)> lrs = stats.GetLetterRatings(
                    alwaysInclude: Generator.alphabet.Union(LetterRating.toInclude).ToHashSet());
            ratingsDrawn = lrs.Count;
            var keys = lrs.Keys.
                OrderBy(c => !lrs[c].active)
                .ThenBy(c => !char.IsLetterOrDigit(c))
                .ThenBy(c => stats.charTimes[c].average);

            foreach (char k in keys)
            {
                new LetterRating(k, lrs[k].color, lrs[k].active);
            }
            RatingsChanged();
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
            RatingsChanged(windowWidth: e.NewSize.Width); 
            e.Handled = true;
        }

        public void RatingsChanged(double windowWidth = 0, double spacing = 2)
        {
            Window w = Window.GetWindow(this);
            if(w != null)
            {

                if (windowWidth == 0)
                {
                    windowWidth = w.WindowState == WindowState.Maximized ? w.ActualWidth : w.Width;
                }
                double margins = letterRatings.Margin.Left + letterRatings.Margin.Right;
                double availableWidth = windowWidth - margins;
                letterRatings.Columns = (int)Math.Ceiling(Math.Min(
                    availableWidth / (LetterRating.width + spacing), ratingsDrawn));
                SetMainRealEstate();
            }
            
        }

        private void SetMainRealEstate()
        {
            Window w = Window.GetWindow(this);
            var realestatemargin = MainRealEstate.Margin;
            realestatemargin.Top = letterRatings.ActualHeight + 110; //HUD height auto doesn't give the proper value, nor does actualheight so we're using this estimation
            MainRealEstate.Margin = realestatemargin;
            w.MinHeight = letterRatings.ActualHeight + Main.ActualHeight + 220; //220 to account for HUD height and margins carved space
        }



        Point lastMousePos;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(Wrapper);

            if ((lastMousePos - mousePos).Length > 2)
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
        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
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
            SetMainRealEstate();

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Trace.WriteLine("Loaded MainPage");
            //Reset();
            UpdateHUD();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Window.GetWindow(this)).LoadSettingsPage();
        }

        private void RerollButton_Click(object sender, RoutedEventArgs e)
        {
            Text = Generator.NextText();
            Reset();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            ResetCursorBlink();
        }

        private void Page_LostFocus(object sender, RoutedEventArgs e)
        {
            HUD.Opacity = 0.75;
            Main.Opacity = 0.25;
            timer.Stop(); //let's be nice and not count the time when we're tabbed away
        }

        private void Page_GotFocus(object sender, RoutedEventArgs e)
        {
            HUD.Opacity = 1;
            Main.Opacity = 1;
            UpdateMain();
            ResetCursorBlink();
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            KeyTrainSerializer.Serialize(stats, CFG["profilePath"]);
            cursorBlinker.Dispose();
        }
    }
}
