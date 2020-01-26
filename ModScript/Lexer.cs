using System;
using System.Collections.Generic;
using System.Linq;

namespace ModScript
{
    class Lexer
    {
        public Error error;
        public List<LToken> tokens = new List<LToken>();
        static string 
            NUMBERS = "0123456789",
            LETTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_",
            LETTERS_NUMBERS = LETTERS + NUMBERS;
        static Dictionary<char, char> escapeChars = new Dictionary<char, char>()
        {
            {'n', '\n'},
            {'t', '\t'}
        };

            
        static string[] KEYWORDS = new string[]
        {
            "let",
            "if",
            "else",
            "while",
            "for",
            "function"
        };

        public Lexer(string fileName, string text)
        {
            TextPosition pos = new TextPosition(0,0,0,fileName, text);
            while(pos.index < text.Length)
            {
                if(" \t".Contains(text[pos.index]))
                {
                    pos.Step();
                    continue;
                }
                switch (text[pos.index])
                {
                    case '+':
                        Arythmia(TokenType.ADD, text, pos);
                        break;
                    case '-':
                        Arythmia(TokenType.SUB, text, pos);
                        break;
                    case '*':
                        Arythmia(TokenType.MULT, text, pos);
                        break;
                    case '/':
                        Arythmia(TokenType.DIV, text, pos);
                        break;
                    case '^':
                        Arythmia(TokenType.POW, text, pos);
                        break;
                    case '%':
                        Arythmia(TokenType.MOD, text, pos);
                        break;
                    case ',':
                        tokens.Add(new LToken(TokenType.COMMA, pos));
                        break;
                    case '(':
                        tokens.Add(new LToken(TokenType.LPAR, pos));
                        break;
                    case ')':
                        tokens.Add(new LToken(TokenType.RPAR, pos));
                        break;
                    case '[':
                        tokens.Add(new LToken(TokenType.LSQBR, pos));
                        break;
                    case ']':
                        tokens.Add(new LToken(TokenType.RSQBR, pos));
                        break;
                    case '{':
                        tokens.Add(new LToken(TokenType.LBRACK, pos));
                        break;
                    case '}':
                        tokens.Add(new LToken(TokenType.RBRACK, pos));
                        break;
                    case ';':
                        tokens.Add(new LToken(TokenType.NLINE, pos));
                        break;
                    case '!':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '=')
                                tokens.Add(new LToken(TokenType.NE, tp));
                            else
                            {
                                pos.Back();
                                tokens.Add(new LToken(TokenType.NOT, tp));
                            }
                            break;
                        }
                    case '=':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '=')
                                tokens.Add(new LToken(TokenType.EE, tp));
                            else if(text[pos.index] == '>')
                                tokens.Add(new LToken(TokenType.ARROW, tp));
                            else
                            {
                                pos.Back();
                                tokens.Add(new LToken(TokenType.EQUAL, tp));
                            }
                            break;
                        }
                    case '<':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '=')
                                tokens.Add(new LToken(TokenType.LTE, tp));
                            else
                            {
                                pos.Back();
                                tokens.Add(new LToken(TokenType.LT, tp));
                            }
                            break;
                        }
                    case '>':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '=')
                                tokens.Add(new LToken(TokenType.GTE, tp));
                            else
                            {
                                pos.Back();
                                tokens.Add(new LToken(TokenType.GT, tp));
                            }
                            break;
                        }
                    case '&':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '&')
                                tokens.Add(new LToken(TokenType.AND, tp));
                            else
                            {
                                error = new BadFormatingError(pos.Copy(), "Character not expected " + text[pos.index].ToString());
                                return;
                            }
                            break;
                        }
                    case '|':
                        {
                            TextPosition tp = pos.Copy();
                            pos.Step();
                            if (text[pos.index] == '|')
                                tokens.Add(new LToken(TokenType.OR, tp));
                            else
                            {
                                error = new BadFormatingError(pos.Copy(), "Character not expected " + text[pos.index].ToString());
                                return;
                            }
                            break;
                        }
                    case '"':
                        {
                            string s = "";
                            TextPosition p = pos.Copy();
                            bool escapeChar = false;
                            pos.Step();
                            while (pos.index < text.Length && (text[pos.index] != '"' || escapeChar))
                            {
                                if (escapeChar)
                                {
                                    escapeChar = false;
                                    if (escapeChars.ContainsKey(text[pos.index]))
                                        s += escapeChars[text[pos.index]];
                                    else
                                        s += text[pos.index];
                                }
                                else
                                {
                                    if (text[pos.index] == '\\')
                                        escapeChar = true;
                                    else
                                        s += text[pos.index];
                                }
                                pos.Step();
                            }
                            if (pos.index == text.Length)
                            {
                                error = new BadFormatingError(p, "Expected '\"'");
                                return;
                            }
                            tokens.Add(new LToken(TokenType.VALUE, new Value(s), p));
                            break;
                        }
                    default:
                        if(NUMBERS.Contains(text[pos.index]))
                        {
                            string num = "";
                            bool hasDot = false;
                            while(pos.index < text.Length && (NUMBERS + ".").Contains(text[pos.index]))
                            {
                                if (text[pos.index] == '.')
                                    if (hasDot)
                                    {
                                        error = new BadFormatingError(pos.Copy(), "Multiple dots");
                                        return;
                                    }
                                    else hasDot = true;
                                num += text[pos.index];
                                pos.Step();
                            }
                            pos.Back();
                            tokens.Add(new LToken(TokenType.VALUE, new Value(Value.GetNumber(num)), pos));
                            break;
                        }
                        else if(LETTERS.Contains(text[pos.index]))
                        {
                            string s = "";
                            TextPosition TP = pos.Copy();
                            while(pos.index < text.Length && LETTERS_NUMBERS.Contains(text[pos.index]))
                            {
                                s += text[pos.index];
                                pos.Step();
                            }
                            pos.Back();
                            tokens.Add(new LToken(KEYWORDS.Contains(s) ? TokenType.KEYWORD : TokenType.IDENTIFIER, new Value(s), TP));
                            break;
                        }
                        error = new BadFormatingError(pos.Copy(), "Character not recognized " + text[pos.index].ToString());
                        return;
                }
                pos.Step();
            }
            tokens.Add(new LToken(TokenType.EOF, pos));

        }


        void Arythmia(TokenType t, string text, TextPosition pos)
        {
            pos.Step();
            if(text[pos.index] == '=')
                tokens.Add(new LToken(t | TokenType.EQUAL, pos));
            else
            {
                pos.Back();
                tokens.Add(new LToken(t, pos));
            }
        }

    }

    class TextPosition
    {
        public int line, character, index;
        public string fName, fContent;
        public TextPosition(int _index, int _line, int _char, string _fName, string _fContent)
        {
            index = _index;
            line = _line;
            character = _char;
            fName = _fName;
            fContent = _fContent;
        }
        public void Step()
        {
            character++;
            index++;
            if (index < fContent.Length)
                if (fContent[index] == '\n')
                {
                    line++;
                    character = 0;
                    Step();
                }
        }
        public void Back()
        {
            character--;
            index--;
            if (index >= 0)
                if (fContent[index] == '\n')
                {
                    line--;
                    character = 1;
                    while(index - character >= 0 && fContent[index - character] != '\n')
                        character++;
                    Back();
                }
        }
        public TextPosition Copy()
        {
            return new TextPosition(index, line, character, fName, fContent);
        }
        public override string ToString()
        {
            return index.ToString();
        }
    }
}
