using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace TextTokenizer
{
    [XmlInclude(typeof(Word))]
    [XmlInclude(typeof(Punctuation))]
    public abstract class Token
    {
        [XmlText]
        public string Value { get; set; }
        protected Token() { }
        protected Token(string value) => Value = value;
        public override string ToString() => Value;
    }

    public class Word : Token
    {
        public Word() : base() { }
        public Word(string value) : base(value) { }
        [XmlIgnore]
        public int Length => Value.Length;
    }

    public class Punctuation : Token
    {
        public Punctuation() : base() { }
        public Punctuation(string value) : base(value) { }
    }

    public class Sentence
    {
        [XmlElement("Word", Type = typeof(Word))]
        [XmlElement("Punctuation", Type = typeof(Punctuation))]
        public List<Token> Tokens { get; set; } = new List<Token>();

        [XmlIgnore]
        public List<Word> Words => Tokens.FindAll(t => t is Word).ConvertAll(t => (Word)t);

        [XmlIgnore]
        public bool IsQuestion => Tokens.Count > 0 && Tokens[^1].Value.Contains("?");

        public override string ToString() => string.Join("", Tokens.ConvertAll(t => t.Value));
    }

    [XmlRoot("text")]
    public class Text
    {
        [XmlElement("sentence")]
        public List<Sentence> Sentences { get; set; } = new List<Sentence>();
    }

    public static class TextParser
    {
        public static Text Parse(string rawText)
        {
            var textObj = new Text();
            var rawSentences = Regex.Split(rawText, @"(?<=[.!?])\s+");

            foreach (var rawSentence in rawSentences)
            {
                if (string.IsNullOrWhiteSpace(rawSentence)) continue;
                var sentence = new Sentence();
                var matches = Regex.Matches(rawSentence, @"(\w+|[^\w\s]+|\s+)");

                foreach (Match match in matches)
                {
                    string val = match.Value;
                    if (Regex.IsMatch(val, @"^\w+$"))
                        sentence.Tokens.Add(new Word(val));
                    else
                        sentence.Tokens.Add(new Punctuation(val));
                }
                textObj.Sentences.Add(sentence);
            }
            return textObj;
        }
    }

    public static class TextProcessor
    {
        public static List<Sentence> SortByWordCount(Text text) =>
            text.Sentences.OrderBy(s => s.Words.Count).ToList();

        public static HashSet<string> FindWordsInQuestions(Text text, int length)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in text.Sentences.Where(s => s.IsQuestion))
            {
                foreach (var w in s.Words.Where(w => w.Length == length))
                    result.Add(w.Value);
            }
            return result;
        }

        public static void RemoveStopWords(Text text, HashSet<string> stopWords)
        {
            foreach (var s in text.Sentences)
            {
                s.Tokens.RemoveAll(t => t is Word w && stopWords.Contains(w.Value.ToLower()));
            }
        }
    }

    public static class TextPresenter
    {
        public static HashSet<string> LoadStopWords(string path)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return stopWords;

            foreach (var line in File.ReadLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    stopWords.Add(line.Trim().ToLower());
            }
            return stopWords;
        }

        public static void PrintOriginal(Text text)
        {
            Console.WriteLine("--- Исходный текст ---");
            text.Sentences.ForEach(s => Console.WriteLine(s));
        }

        public static void PrintSortedByWords(Text text)
        {
            Console.WriteLine("\n--- 1. Сортировка по числу слов ---");
            var sorted = TextProcessor.SortByWordCount(text);
            sorted.ForEach(s => Console.WriteLine($"{s.Words.Count} слов: {s.ToString().Trim()}"));
        }

        public static void PrintQuestionWords(Text text, int length)
        {
            Console.WriteLine($"\n--- 3. Слова с длиной {length} в вопросах ---");
            var words = TextProcessor.FindWordsInQuestions(text, length);
            Console.WriteLine(string.Join(", ", words));
        }

        public static void PrintAfterFilter(Text text)
        {
            Console.WriteLine("\n--- 6. После удаления стоп-слов ---");
            text.Sentences.ForEach(s => Console.WriteLine(s));
        }

        public static void ExportToXml(Text text, string path)
        {
            var serializer = new XmlSerializer(typeof(Text));
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, text);
            }
            Console.WriteLine($"\n--- 7. Экспорт в XML завершен в файл {path} ---");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string sampleText = "Привет мир! Это текст учебника по программированию? " +
                                "Мы пишем код лабораторной работы номер три.";

            var stopWords = TextPresenter.LoadStopWords("stopwords_ru.txt");
            var text = TextParser.Parse(sampleText);

            TextPresenter.PrintOriginal(text);
            TextPresenter.PrintSortedByWords(text);
            TextPresenter.PrintQuestionWords(text, length: 5);

            TextProcessor.RemoveStopWords(text, stopWords);
            TextPresenter.PrintAfterFilter(text);
            TextPresenter.ExportToXml(text, "output.xml");
        }
    }
}
