using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using static Pythonic.ListHelpers;

namespace KeyTrain
{
    
    /// <summary>
    /// Abstract class for generating keyboard lessons
    /// </summary>
    public abstract class LessonGenerator
    {
        protected static int defaultLessonLength => ConfigManager.lessonLength;
        public abstract string CurrentText { get; }
        public abstract string NextText();
        public abstract HashSet<char> alphabet { get; protected set; }
    }
    /// <summary>
    /// Keyboard lessons with predefined text, split into chunks
    /// </summary>
    class PresetTextLesson : LessonGenerator
    {
        static List<string> queuedTexts;
        static int currTextid = 0;
        public override string CurrentText => queuedTexts[currTextid % queuedTexts.Count].Trim();
        public override HashSet<Char> alphabet{ get; protected set;}

        public PresetTextLesson(string text, int maxLength = 0)
        {
            if (maxLength == 0) maxLength = defaultLessonLength;
            text = text.Trim();

            alphabet = text.ToUpper().ToHashSet();

            queuedTexts = new List<string>();

            //Divide up the text into about to equal chunks, each of which is smaller than maxLength
            int start = 0;
            int remaining = text.Length;
            do
            {
                double chunks = Math.Ceiling((double)remaining / maxLength);
                int targetLength = (int)Math.Ceiling(remaining / chunks);

                int chunklength = text.Substring(start, targetLength).LastIndexOf(" ");
                if(chunklength == - 1) //word is too long; chop it up
                {
                    chunklength = start + maxLength;
                }
                // .Substring complains about going over the length
                queuedTexts.Add(text.Substring(start, Math.Min( chunklength, remaining))); 
                start += chunklength + 1;
                remaining = text.Length - start;
            } while (remaining > 0);



        }

        public override string NextText()
        {
            currTextid++;
            return CurrentText;
        }
    }

    /// <summary>
    /// Keyboard lessons with randomly generated text, emphasizing certain keys as you need
    /// </summary>
    class RandomizedLesson : LessonGenerator
    {
        private string text;
        private List<string> dict;
        private List<string> shuffled;
        private int place = 0;
        private int chunkLength;
        private List<char> punctuation = new List<char>();
        Random random;
        public static int seed = 0; //different on startup
        public static int offset = 0; //keep from skipping back and forth abusing the shuffle button
        Random NextRandom() => new Random(seed + MainPage.stats.WPMLOG.Count + offset); //
        const int minSampleSize = 10; //minimum amount of words deemed enough to pick from and seem random
        int capitals => ConfigManager.Settings["capitalsLevel"];

        /// <summary>
        /// Cleans dictionary of unwanted elements eg. control characters, too short or too long words
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private List<string> Sanitize(IEnumerable<string> dirty)
        {
            return dirty.Where(w => w.Length > 2).ToList();
        }


        public override string CurrentText => text.Trim();
        public override string NextText()
        {
            text = "";

            while (true)
            {
                string nextWord =  shuffled[++place % shuffled.Count];
                switch (capitals) //0: force lower, 1: keep existing, 2: 50% first letter  3: first letter, 4: all caps
                {
                    default:
                        break;
                    case 0:
                        nextWord = nextWord.ToLower();
                        break;
                    case 2:
                        if(random.NextDouble() > 0.5)
                            nextWord = nextWord.Substring(0, 1).ToUpper() + nextWord.Substring(1);
                        break;
                    case 3:
                        nextWord = nextWord.Substring(0, 1).ToUpper() + nextWord.Substring(1);
                        break;
                    case 4:
                        nextWord = nextWord.ToUpper();
                        break;
                }
                if(text == "")
                {
                    text = nextWord;
                }
                else if (text.Length + nextWord.Length + 3 < chunkLength)
                {
                    char pct = punctuation.ElementAtOrDefault(random.Next(punctuation.Count + 1)); //+1 causes invalid indexes so we still generate just spaces some of the time
                    string sep = " ";
                    if (pct != '\0' && pct != text.Last())
                    {
                        sep = pct == '-' ? pct.ToString() : pct + " ";
                    }
                        
                    text = string.Join(sep, text, nextWord);
                }
                else
                {
                    break;
                }

            }
            text = text.Trim();
            
            return text;
        }
        /// <summary>
        /// The set of characters that the dictionary uses more than 50 times
        /// </summary>
        public override HashSet<char> alphabet { get; protected set; }

        /// <summary>
        /// Sets the options to only contain words that contain all given characters, or if that's not enough, any of the given characters.
        /// Emphasizing punctuation characters means they will be randomly inserted between words by NextText().
        /// Emphasizing whitespace means the generator will favor shorter words
        /// </summary>
        /// <param name="emphasized">Set of words to emphasize</param>
        public void Emphasize(string emphasized, int minSampleSize = minSampleSize)
        {
            var normal = emphasized.Where(c => char.IsLetterOrDigit(c));
            var options = ConcatToList<IEnumerable<string>>(
                dict.Where(word => normal.All(e => word.ToUpper().Contains(e))),
                dict.Where(word => normal.Any(e => word.ToUpper().Contains(e))),
                dict ).First(d => d.Count() > minSampleSize).ToList();
            punctuation = emphasized.Where(c => char.IsPunctuation(c)).ToList();
            if(emphasized.Any(c => char.IsWhiteSpace(c)))
            {
                options = options.OrderBy(w => w.Length).Take(Math.Max(options.Count / 100, 150)).ToList();
            }
            random = NextRandom();
            place = 0;
            shuffled = options.OrderBy(x => random.Next()).ToList();
        }

        /// <param name="dictonary">List of words the generator can use</param>
        /// <param name="maxLength">Exclusive maximum length of each generated text chunk</param>
        public RandomizedLesson(IEnumerable<string> dictonary, int maxLength = 0)
        {
            if (maxLength == 0) maxLength = defaultLessonLength;

            dict = Sanitize(dictonary);
            chunkLength = maxLength;
            random = NextRandom();
            place = 0;
            shuffled = dict.OrderBy(x=> random.Next()).ToList();

            //all characters which appear at least 50 times in the dictionary
            alphabet = dict.SelectMany(x => x.ToUpper()).GroupBy(x => x).Where(x => x.Count() > 50).SelectMany(x => x).ToHashSet();
            NextText();
        }

        /// <param name="dictionaryFile">Path to text file that contains all the words the generator can use (1 word/phrase per line)</param>
        /// <param name="maxlength">Exclusive maximum length of each generated text chunk</param>
        public RandomizedLesson(string dictionaryFile = "Resources/dictionaryEN.txt", int maxlength = 0) :
            this(File.ReadAllLines(dictionaryFile).ToList(), maxlength) {}

        /// <param name="dictionaryFiles">List of text files that contain all the words the generator can use (1 word/phrase per line)</param>
        /// <param name="maxlength">Exclusive maximum length of each generated text chunk</param>
        /// 
        public static RandomizedLesson FromDictionaryFiles(IEnumerable<string> dictionaryFiles, int maxlength = 0)
        {
            if (maxlength == 0) maxlength = defaultLessonLength;
            return new RandomizedLesson(dictionaryFiles.SelectMany(f => File.ReadAllLines(f)).ToList(), maxlength);
        }
        public static RandomizedLesson FromDictionaryFiles(string dictionaryFile, int maxlength = 0)
        {
            if (maxlength == 0) maxlength = defaultLessonLength;
            return new RandomizedLesson(File.ReadAllLines(dictionaryFile).ToList(), maxlength);
        }

    }

}
