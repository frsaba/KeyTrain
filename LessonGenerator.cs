using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Pythonic.ListHelpers;

namespace KeyTrain
{
    /// <summary>
    /// Abstract class for generating keyboard lessons
    /// </summary>
    abstract class LessonGenerator
    {
        public const int defaultLessonLength = 45;
        public abstract string CurrentText { get; }
        public abstract string NextText();
        public abstract HashSet<Char> alphabet { get; protected set; }
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

        public PresetTextLesson(string text, int maxLength = defaultLessonLength)
        {
            text = text.Trim();

            alphabet = new HashSet<char>();
            foreach (char ch in text.ToUpper())
            {
                alphabet.Add(ch);
            }

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
        private List<string> options;
        private int length;
        Random random;

        public override string CurrentText => text.Trim();
        public override string NextText()
        {
            text = "";

            while (true)
            {
                string nextWord =  options[random.Next(options.Count)] ;
                if (text.Length + nextWord.Length < length)
                {
                    text = string.Join(' ', text, nextWord);
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
        /// Sets the options to only contain words that contain all given characters, or if that's not enough, any of the given characters
        /// </summary>
        /// <param name="emphasized">Set of words to emphasize</param>
        public void Emphasize(HashSet<char> emphasized)
        {
            options = ConcatToList<List<string>>(
                dict.Where(word => emphasized.All(e => word.ToUpper().Contains(e))).ToList(),
                dict.Where(word => emphasized.Any(e => word.ToUpper().Contains(e))).ToList(),
                dict ).First(d => d.Count > 10);
            
        }

        /// <param name="dictonary">List of words the generator can use</param>
        /// <param name="maxlength">Exclusive maximum length of each generated text chunk</param>
        public RandomizedLesson(List<string> dictonary, int maxlength = defaultLessonLength)
        {
            dict = dictonary;
            options = dict;
            length = maxlength;
            random = new Random();

            //all characters which appear at least 50 times in the dictionary
            alphabet = dict.SelectMany(x => x.ToUpper()).GroupBy(x => x).Where(x => x.Count() > 50).SelectMany(x => x).ToHashSet();
            NextText();
        }

        /// <param name="dictionaryFile">Path to text file that contains all the words the generator can use (1 word/phrase per line)</param>
        /// <param name="maxlength">Exclusive maximum length of each generated text chunk</param>
        public RandomizedLesson(string dictionaryFile = "Resources/dictionaryHU.txt", int maxlength = defaultLessonLength) :
            this(File.ReadAllLines(dictionaryFile).ToList(), maxlength) {}

    }

}
