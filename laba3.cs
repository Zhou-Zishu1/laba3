using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace TextTokenizer
{
    class Program
    {
        static void Main(string[] args)
        {
            TextAppRunner.RunApp("input.txt", "stopwords.txt", "output.xml");
            Console.ReadLine();
        }
    }

    public static class TextAppRunner
    {
        public static void RunApp(string textFile, string stopWordsFile, string xmlOutputFile)
        {
            if (!File.Exists(textFile)) 
                File.WriteAllText(textFile, "Hello world! Is this a good sample text?! Yes, it absolutely is... Мама мыла красивую раму с мылом.");
            if (!File.Exists(stopWordsFile)) 
                File.WriteAllText(stopWordsFile, "с\nа\nit\nis");

            string sourceText = File.ReadAllText(textFile);
            var stopWords = new HashSet<string>(File.ReadAllLines(stopWordsFile).Select(w => w.Trim().ToLower()));

            Console.WriteLine($"--- Source Text from File '{textFile}': ---\n{sourceText}\n");

            Text text = TextParser.Parse(sourceText);

            TextProcessor.PrintByWordCount(text);
            TextProcessor.PrintByLength(text);
            TextProcessor.FindWordsInQuestions(text, 4);

            if (text.Sentences.Any())
                TextProcessor.ReplaceWordsInSentence(text.Sentences.First(), 4, "[REPLACED]"); 

            TextProcessor.RemoveWordsStartingWithConsonant(text, 5);
            TextProcessor.RemoveStopWords(text, stopWords);

            Console.WriteLine("\n--- Text After All Structural Modifications: ---");
            foreach (var s in text.Sentences) Console.WriteLine(s);

            TextProcessor.ExportToXml(text, xmlOutputFile);
        }
    }

    public abstract class TokenBase 
    { 
        public string Value { get; set; } 
    }

    public class Word : TokenBase 
    {
        public Word() { }
        public Word(string value) => Value = value;
    }

    public class Punctuation : TokenBase 
    {
        public Punctuation() { }
        public Punctuation(string value) => Value = value;
    }

    public class XmlWordSurrogate
    {
        [XmlText] 
        public string Value { get; set; }
        public XmlWordSurrogate() { }
        public XmlWordSurrogate(string value) => Value = value;
    }

    public class Sentence
    {
        [XmlIgnore]
        public List<TokenBase> Tokens { get; set; } = new List<TokenBase>();

        [XmlElement("word")]
        public List<XmlWordSurrogate> XmlWords
        {
            get => Tokens.OfType<Word>().Select(w => new XmlWordSurrogate(w.Value)).ToList();
            set => Tokens = value?.Select(xw => new Word(xw.Value)).Cast<TokenBase>().ToList() ?? new List<TokenBase>();
        }

        [XmlIgnore]
        public List<Word> Words => Tokens.OfType<Word>().ToList();
        
        [XmlIgnore]
        public bool IsQuestion => Tokens.LastOrDefault()?.Value?.Contains("?") ?? false;

        public override string ToString() 
        {
            if (Tokens == null || !Tokens.Any()) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < Tokens.Count; i++)
            {
                var current = Tokens[i];
                if (current == null || string.IsNullOrEmpty(current.Value)) continue;

                if (current is Word && sb.Length > 0)
                {
                    char lastChar = sb[sb.Length - 1];
                    if (lastChar != '(' && lastChar != '[' && lastChar != '{')
                    {
                        sb.Append(' ');
                    }
                }
                sb.Append(current.Value);
            }
            return sb.ToString();
        }
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
            if (string.IsNullOrWhiteSpace(rawText)) return textObj;

            var sentenceMatches = Regex.Split(rawText, @"(?<=[.!?])\s+");

            foreach (var sStr in sentenceMatches)
            {
                if (string.IsNullOrWhiteSpace(sStr)) continue;

                var sentence = new Sentence();
                var tokens = Regex.Matches(sStr, @"[a-zA-Z0-9а-яА-ЯёЁ]+|[^\w\s]+");

                foreach (Match match in tokens)
                {
                    string val = match.Value;
                    if (string.IsNullOrEmpty(val)) continue;

                    if (char.IsLetterOrDigit(val[0]))
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
        public static void PrintByWordCount(Text text)
        {
            Console.WriteLine("--- 1. Sorted by Word Count ---");
            var sorted = text.Sentences.OrderBy(s => s.Words.Count);
            foreach (var s in sorted) Console.WriteLine($"[{s.Words.Count} words]: {s}");
        }

        public static void PrintByLength(Text text)
        {
            Console.WriteLine("\n--- 2. Sorted by Sentence Length ---");
            var sorted = text.Sentences.OrderBy(s => s.ToString().Length);
            foreach (var s in sorted) Console.WriteLine($"[{s.ToString().Length} chars]: {s}");
        }

        public static void FindWordsInQuestions(Text text, int length)
        {
            Console.WriteLine($"\n--- 3. Unique Words of Length {length} in Interrogative Sentences ---");
            var words = text.Sentences
                .Where(s => s.IsQuestion)
                .SelectMany(s => s.Words)
                .Where(w => !string.IsNullOrEmpty(w.Value) && w.Value.Length == length)
                .Select(w => w.Value.ToLower())
                .Distinct();
            Console.WriteLine(words.Any() ? string.Join(", ", words) : "No matching words found.");
        }

        public static void RemoveWordsStartingWithConsonant(Text text, int length)
        {
            string consonants = "bcdfghjklmnpqrstvwxyzбвгджзйклмнпрстфхцчшщ";
            foreach (var s in text.Sentences)
            {
                s.Tokens.RemoveAll(t => t is Word w && 
                                        !string.IsNullOrEmpty(w.Value) && 
                                        w.Value.Length == length && 
                                        consonants.Contains(char.ToLower(w.Value[0])));
            }
            text.Sentences.RemoveAll(s => !s.Words.Any());
            Console.WriteLine($"\n--- 4. Words of Length {length} Starting with Consonant Removed ---");
        }

        public static void ReplaceWordsInSentence(Sentence sentence, int length, string subStr)
        {
            if (sentence == null) return;
            foreach (var token in sentence.Tokens.OfType<Word>().Where(w => !string.IsNullOrEmpty(w.Value) && w.Value.Length == length))
            {
                token.Value = subStr;
            }
            Console.WriteLine($"\n--- 5. Words of Length {length} Replaced with '{subStr}' in the Sentence ---");
        }

        public static void RemoveStopWords(Text text, HashSet<string> stopWords)
        {
            foreach (var s in text.Sentences)
            {
                s.Tokens.RemoveAll(t => t is Word w && !string.IsNullOrEmpty(w.Value) && stopWords.Contains(w.Value.ToLower()));
            }
            text.Sentences.RemoveAll(s => !s.Words.Any());
            Console.WriteLine("\n--- 6. Stop Words Successfully Removed ---");
        }

        public static void ExportToXml(Text text, string filePath)
        {
            var serializer = new XmlSerializer(typeof(Text));
            var settings = new XmlWriterSettings 
            { 
                Encoding = Encoding.UTF8, 
                Indent = true 
            };

            using (var writer = XmlWriter.Create(filePath, settings))
            {
                serializer.Serialize(writer, text);
            }
            Console.WriteLine($"\n--- 7. XML Output Successfully Saved to: {filePath} ---");
        }
    }
}
