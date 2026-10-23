using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TextTokenizer
{
    // Базовые сущности для токенизации текста
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
}
