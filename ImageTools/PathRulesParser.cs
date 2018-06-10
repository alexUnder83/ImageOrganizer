using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageTools {
    public static class PathRulesParser {
        public static List<Token> Parse(string pathRules) {
            List<Token> result = new List<Token>();
            StringBuilder token = new StringBuilder();
            Stack<Token> current = new Stack<Token>();
            current.Push(new Token());
            result.Add(current.Peek());
            int length = pathRules.Length;
            for (int index = 0; index < length; index++) {
                char ch = pathRules[index];
                switch (ch) {
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        continue;
                    case '\\':
                        if (token.Length > 0) {
                            EndToken(current, token.ToString().Trim());
                            result.Add(current.Peek());
                            token.Length = 0;
                        }
                        break;
                    case ':':
                        if (token.Length > 0) {
                            current.Peek().ConditionFormat = token.ToString().Trim();
                            token.Length = 0;
                        }
                        break;
                    case '|':
                        if (token.Length > 0) {
                            NextToken(current, token.ToString());
                            token.Length = 0;
                        }
                        break;
                    case '\'':
                        int endTextIndex = SkipCharacters(pathRules, index, '\'');
                        token.Append(pathRules.Substring(index, endTextIndex - index + 1));
                        index = endTextIndex;
                        break;
                    case '(':
                        int endFilterIndex = SkipCharacters(pathRules, index, ')');
                        current.Peek().Filter = pathRules.Substring(index + 1, endFilterIndex - index - 1).Trim();
                        index = endFilterIndex;
                        break;
                    case ',':
                        if (token.Length > 0) {
                            StartToken(current, token.ToString());
                            token.Length = 0;
                        }
                        break;
                    default:
                        token.Append(ch);
                        break;
                }
            }
            if (token.Length > 0)
                EndToken(current, token.ToString().Trim());

            return result;
        }
        static int SkipCharacters(string tokens, int startIndex, char terminateChar) {
            int length = tokens.Length;
            int endIndex = startIndex;
            do {
                endIndex++;
            } while (endIndex < length && tokens[endIndex] != terminateChar);
            return endIndex;
        }
        static void StartToken(Stack<Token> current, string conditionFormat) {
            current.Peek().ConditionFormat = conditionFormat.Trim();
            Token nextToken = new Token();
            current.Peek().Next = nextToken;
            current.Push(nextToken);
        }
        static void NextToken(Stack<Token> current, string displayFormat) {
            Token currentToken = current.Peek();
            EndToken(current, displayFormat.Trim());
            currentToken.Next = current.Peek();
        }
        static void EndToken(Stack<Token> current, string displayFormat) {
            while (current.Count > 0)
                current.Pop().DisplayFormat = displayFormat;
            current.Push(new Token());
        }
    }
    public class Token {
        string displayFormat;
        string conditionFormat;
        string filter;
        Token next;

        public Token(string displayFormat, string ConditionFormat) {
            this.displayFormat = displayFormat;
            this.ConditionFormat = ConditionFormat;
        }
        public Token(string displayFormat)
            : this(displayFormat, null) {
        }
        public Token() {
        }

        public string DisplayFormat { get { return displayFormat; } set { displayFormat = value; } }
        public string ConditionFormat { get { return conditionFormat; } set { conditionFormat = value; } }
        public string Filter { get { return filter; } set { filter = value; } }
        public Token Next { get { return next; } set { next = value; } }

        public override string ToString() {
            return !String.IsNullOrEmpty(ConditionFormat) ? ConditionFormat + ":" + DisplayFormat : DisplayFormat;
        }
    }
}
