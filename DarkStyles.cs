using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;

namespace KeyTrainWPF
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

            public static SectionStyle missedStyle = new SectionStyle(fgColor: Color.FromRgb(201, 23, 10));
            public static SectionStyle typedstyle = new SectionStyle(fgColor: Colors.Gray);

            public static (Color, Color) activeBgColors = (Colors.Silver, wrapperBackground);
            public static (Color, Color) activeFgColors = (Colors.Black, Colors.White);

            public const string spaceReplacement = "·"; //could be "␣" but it takes up 2 spaces which is a weird look)

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
                public static int width {get; private set;}= 45;
                public static int height {get; private set;} = 45;
                protected static SolidColorBrush normalColor = new SolidColorBrush(Colors.Black);
                protected static SolidColorBrush inactiveColor = new SolidColorBrush(Colors.Gray);
                protected static SolidColorBrush highlightBorder = new SolidColorBrush(Colors.White);

                protected void SetLabelStyle(ref Label l, Color bgcolor, bool hasData)
                {
                    l.Width = width;
                    l.Height = height;

                    l.HorizontalContentAlignment = HorizontalAlignment.Center;
                    l.VerticalContentAlignment = VerticalAlignment.Center;
                    l.FontSize = 30;
                    l.FontWeight = FontWeights.DemiBold;
                    l.Background = new SolidColorBrush(bgcolor);
                    l.FontWeight = FontWeights.Normal;

                    l.BorderThickness = new Thickness(1);
                }
            }

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
