using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Pythonic.ListHelpers;

namespace KeyTrainWPF
{
    abstract class LessonGenerator
    {
        public const int defaultLessonLength = 45;
        public abstract string CurrentText { get; }
        public abstract string NextText();
    }

    class PresetTextLesson : LessonGenerator
    {
        static List<string> queuedTexts;
        static int currTextid = 0;
        public override string CurrentText => queuedTexts[currTextid % queuedTexts.Count].Trim();
        public PresetTextLesson(string text, int maxLength = defaultLessonLength)
        {
            text = text.Trim();
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

        public void Emphasize(HashSet<char> emphasized)
        {
            options = ConcatToList<List<string>>(
                dict.Where(word => emphasized.All(e => word.ToUpper().Contains(e))).ToList(),
                dict.Where(word => emphasized.Any(e => word.ToUpper().Contains(e))).ToList(),
                dict ).First(d => d.Count > 0);
            
        }
        

        public RandomizedLesson(List<string> dictonary, int maxlength = defaultLessonLength)
        {
            dict = dictonary;
            options = dict;
            length = maxlength;
            random = new Random();
            NextText();
        }

        public RandomizedLesson(string dictionaryFile = "Resources/dictionaryHU.txt", int maxlength = defaultLessonLength) :
            this(File.ReadAllLines(dictionaryFile).ToList(), maxlength) {}

    }

}
