using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeyTrain
{
    /// <summary>
    /// Defines MainWindow's dark theme styling.
    /// I don't actually know if this is the way to do this but MainWindow.xaml.cs was getting 
    /// cluttered with all the style information so I moved it out here
    /// Actually, TODO: move this to an external JSON file or something
    /// </summary>
    public class DarkStyles
    {
        public partial class MainWindow : Window
        {
            public static Color wrapperBackground { get; private set; } = (Color)ColorConverter.ConvertFromString("#1f1f1f");
            public static List<(Color color, double delta)> gainPalette { get; private set; } =
                new List<(Color, double)>
                {
                    (Colors.Tomato,      -0.5),
                    (Colors.Silver,         0.2),
                    (Colors.LightGreen,    double.MaxValue)
                };

            public static SectionStyle 
                 errorStyle     = new SectionStyle(fgColor: Color.FromRgb(201, 23, 10)),
                 typedStyle     = new SectionStyle(fgColor: Colors.Gray),
                 mistakesStyle  = new SectionStyle(bgColor: Color.FromRgb(201, 23, 10)),
                 activeStyle    = new SectionStyle(fgColor: Colors.Black, bgColor: Colors.Silver),
                 remainingStyle = new SectionStyle();

            public static TimeSpan blinkTime = TimeSpan.FromMilliseconds(600);
            public static (Color, Color) cursorBgColors = (Colors.Silver, wrapperBackground);
            public static (Color, Color) cursorFgColors = (Colors.Black, Colors.White);

            public const string spaceReplacement = "·"; //could be "␣" but it takes up 2 spaces which is a weird look)
            public const int stdTooltipDelay = 750;
            public class SectionStyle
            {
                public string startText;
                public Color fg;
                public Color bg;

                public SectionStyle(string text = "", Color? fgColor = null, Color? bgColor = null)
                {
                    startText = text;
                    fg = fgColor ?? Colors.White;
                    bg = bgColor ?? wrapperBackground;
                }
            }

            public class LetterRatingStyle
            {
                public static int width {get; private set;}= 37;
                public static int height {get; private set;} = 40;
                public static int fontSize {get; private set;} = 25;
                protected static SolidColorBrush 
                    normalColor = new SolidColorBrush(Colors.Black),
                    inactiveColor = new SolidColorBrush(Colors.Gray),
                    highlightBorder = new SolidColorBrush(Colors.White);

                protected Label LabelWithStyle(Color bgcolor, bool hasData)
                {
                    Label l = new Label();

                    l.Width = width;
                    l.Height = height;

                    l.HorizontalContentAlignment = HorizontalAlignment.Center;
                    l.VerticalContentAlignment = VerticalAlignment.Center;
                    l.FontSize = fontSize;
                    l.FontWeight = FontWeights.DemiBold;
                    l.Background = new SolidColorBrush(bgcolor);
                    l.FontWeight = FontWeights.Normal;
                    l.Cursor = Cursors.Hand;

                    l.BorderThickness = new Thickness(2);

                    return l;
                }
            }

            //TODO:generalize this, ratios / customizable thresholds
            /// <summary>
            /// Format run based on the value inside
            /// </summary>
            /// <param name="run">The Run to writo to</param>
            /// <param name="value">Value to display</param>
            /// <param name="inverted">True: positve = worse</param>
            public static void ConditionalFormat(Run run, double value, bool inverted = false)
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

            /// <summary>
            /// Creates a new Run with the given SectionStyle and text
            /// </summary>
            /// <returns></returns>
            public static Run RunWithStyle(SectionStyle style = null, string text = "")
            {
                style ??= new SectionStyle();
                Run r = new Run(text);
                r.Foreground = new SolidColorBrush(style.fg);
                r.Background = new SolidColorBrush(style.bg);
                return r;
            }


        }

        
    }

    
}
