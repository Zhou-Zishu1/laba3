using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace TextTokenizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Parser added successfully.");
            Console.ReadLine();
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
}
