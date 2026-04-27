/*
 * Copyright (c) 2006, Adam Dunkels
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the author nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */

using System;
using TracerLibrary;
using System.Collections.Generic;
using System.Diagnostics;

namespace Altair
{
    public class Tokenizer
    {
        #region Fields

        public enum Token : int
        {
            TOKENIZER_NULL = -1,
            TOKENIZER_NONE = 0,
            TOKENIZER_ERROR = 1,
            TOKENIZER_ENDOFINPUT,
            TOKENIZER_INTEGER,
            TOKENIZER_NUMBER,
            TOKENIZER_STRING,
            TOKENIZER_INTERGER_VARIABLE,
            TOKENIZER_STRING_VARIABLE,
            TOKENIZER_NUMERIC_VARIABLE,
            TOKENIZER_NUMERIC_ARRAY_VARIABLE,
            TOKENIZER_STRING_ARRAY_VARIABLE,
            TOKENIZER_DATA,
            TOKENIZER_DEF,
            TOKENIZER_FN,
            TOKENIZER_DIM,
            TOKENIZER_GOSUB,
            TOKENIZER_GOTO,
            TOKENIZER_IF,
            TOKENIZER_INPUT,
            TOKENIZER_LET,
            TOKENIZER_ON,
            TOKENIZER_OPTION,
            TOKENIZER_PRINT,
            TOKENIZER_RESTORE,
            TOKENIZER_READ,
            TOKENIZER_REM,
            TOKENIZER_TAB,
            TOKENIZER_FOR,
            TOKENIZER_TO,
            TOKENIZER_NEXT,
            TOKENIZER_THEN,
            TOKENIZER_RETURN,
            TOKENIZER_END,
            TOKENIZER_STOP,
            TOKENIZER_COMMA,
            TOKENIZER_COLON,
            TOKENIZER_SEMICOLON,
            TOKENIZER_PLUS,
            TOKENIZER_MINUS,
            TOKENIZER_AMPERSAND,
            TOKENIZER_BAR,
            TOKENIZER_ASTR,
            TOKENIZER_SLASH,
            TOKENIZER_MOD,
            TOKENIZER_HASH,
            TOKENIZER_LEFTPAREN,
            TOKENIZER_RIGHTPAREN,
            TOKENIZER_LT,
            TOKENIZER_GT,
            TOKENIZER_EQ,
            TOKENIZER_LTEQ,
            TOKENIZER_GTEQ,
            TOKENIZER_NOTEQ,
            TOKENIZER_CR,
            TOKENIZER_LF,
            TOKENIZER_DOLLAR,
            TOKENIZER_FUNCTION,
            TOKENIZER_QUOTE,
            TOKENIZER_STEP,
            TOKENIZER_EXPONENT,
            TOKENIZER_SQR,
            TOKENIZER_INT,
            TOKENIZER_SIN,
            TOKENIZER_COS,
            TOKENIZER_TAN,
            TOKENIZER_RND,
            TOKENIZER_ABS,
            TOKENIZER_ATN,
            TOKENIZER_EXP,
            TOKENIZER_LOG,
            TOKENIZER_RANDOMIZE,
            TOKENIZER_CHR,
            TOKENIZER_RIGHT,
            TOKENIZER_LEFT,
            TOKENIZER_MID,
            TOKENIZER_NOT,
            //TOKENIZER_POS,
            //TOKENIZER_SPC,
            TOKENIZER_VAL,
            TOKENIZER_ASC,
            TOKENIZER_LEN,
            TOKENIZER_SGN,
            TOKENIZER_STR,
			TOKENIZER_AND,
            TOKENIZER_OR,
            TOKENIZER_XOR,
        };

        int ptr;
        int nextptr;
        readonly char[] source;
        const int MaximumNumberLength = 15;
        Token currentToken = Token.TOKENIZER_ERROR;

        public struct TokenKeyword
        {
            private readonly string keyword;
            private readonly Token token;

            public TokenKeyword(string keyword, Token token)
            {
                this.keyword = keyword;
                this.token = token;
            }

            public string Keyword { get { return keyword; } }
            public Token Token { get { return token; } }
        }

        public readonly List<TokenKeyword> keywords;

        #endregion
        #region Constructors

        public Tokenizer(char[] program)
        {
            Debug.WriteLine("In Tokenizer()");

            //by default, read from/write to standard streams

            keywords = new List<TokenKeyword>(
            new[]
            {
                new  TokenKeyword("dim", Token.TOKENIZER_DIM),
                new  TokenKeyword("let", Token.TOKENIZER_LET),
                new  TokenKeyword("print", Token.TOKENIZER_PRINT),
                new  TokenKeyword("if", Token.TOKENIZER_IF),
                new  TokenKeyword("then", Token.TOKENIZER_THEN),
                new  TokenKeyword("for", Token.TOKENIZER_FOR),
                new  TokenKeyword("to", Token.TOKENIZER_TO),
                new  TokenKeyword("step", Token.TOKENIZER_STEP),
                new  TokenKeyword("next", Token.TOKENIZER_NEXT),
                new  TokenKeyword("goto", Token.TOKENIZER_GOTO),
                new  TokenKeyword("go to", Token.TOKENIZER_GOTO),
                new  TokenKeyword("gosub", Token.TOKENIZER_GOSUB),
                new  TokenKeyword("return", Token.TOKENIZER_RETURN),
                new  TokenKeyword("rem", Token.TOKENIZER_REM),
                new  TokenKeyword("end", Token.TOKENIZER_END),
                new  TokenKeyword("tab", Token.TOKENIZER_TAB),
                new  TokenKeyword("sqr", Token.TOKENIZER_SQR),
                new  TokenKeyword("int", Token.TOKENIZER_INT),
                new  TokenKeyword("read", Token.TOKENIZER_READ),
                new  TokenKeyword("data", Token.TOKENIZER_DATA),
                new  TokenKeyword("stop", Token.TOKENIZER_STOP),
                new  TokenKeyword("sin", Token.TOKENIZER_SIN),
                new  TokenKeyword("cos", Token.TOKENIZER_COS),
                new  TokenKeyword("tan", Token.TOKENIZER_TAN),
                new  TokenKeyword("rnd", Token.TOKENIZER_RND),
                new  TokenKeyword("input", Token.TOKENIZER_INPUT),
                new  TokenKeyword("def", Token.TOKENIZER_DEF),
                new  TokenKeyword("fn", Token.TOKENIZER_FN),
                new  TokenKeyword("abs", Token.TOKENIZER_ABS),
                new  TokenKeyword("atn", Token.TOKENIZER_ATN),
                new  TokenKeyword("exp", Token.TOKENIZER_EXP),
                new  TokenKeyword("log", Token.TOKENIZER_LOG),
                new  TokenKeyword("restore", Token.TOKENIZER_RESTORE),
                new  TokenKeyword("on", Token.TOKENIZER_ON),
                new  TokenKeyword("randomize", Token.TOKENIZER_RANDOMIZE),
                new  TokenKeyword("and", Token.TOKENIZER_AND),
                new  TokenKeyword("or", Token.TOKENIZER_OR),
                new  TokenKeyword("xor", Token.TOKENIZER_XOR),
                new  TokenKeyword("chr$", Token.TOKENIZER_CHR),
                new  TokenKeyword("left$", Token.TOKENIZER_LEFT),
                new  TokenKeyword("mid$", Token.TOKENIZER_MID),
                new  TokenKeyword("not", Token.TOKENIZER_NOT),
                new  TokenKeyword("right$", Token.TOKENIZER_RIGHT),
                new  TokenKeyword("str$", Token.TOKENIZER_STR),
                new  TokenKeyword("sgn", Token.TOKENIZER_SGN),
                new  TokenKeyword("asc", Token.TOKENIZER_ASC),
                new  TokenKeyword("len", Token.TOKENIZER_LEN),
                //new  TokenKeyword("pos", Token.TOKENIZER_POS),
				//new  TokenKeyword("spc", Token.TOKENIZER_SPC),
                new  TokenKeyword("val", Token.TOKENIZER_VAL),
                new  TokenKeyword("null", Token.TOKENIZER_ERROR)
            });
            this.source = program;
            Debug.WriteLine("Out Tokenizer()");
        }

        #endregion
        #region Methods

        public void AcceptToken(Token token)
        {
            Debug.WriteLine("In AcceptToken()");
            if (token != GetToken())
            {
                Expected("expected " + token + ", got " + GetToken());
            }
            TraceInternal.TraceVerbose("accept: Expected " + token + ", got it");
            NextToken();
            Debug.WriteLine("Out AcceptToken()");
        }
        
        public Token CheckSingleChar()
        {
            Debug.WriteLine("In CheckSingleChar()");

            Token token = 0;
            if(source[ptr] == '\n')
            {
                token =  Token.TOKENIZER_CR;
            }
            else if (source[ptr] == '\r')
            {
                token = Token.TOKENIZER_LF;
            }
            else if(source[ptr] == ',')
            {
                token =  Token.TOKENIZER_COMMA;
            }
            else if (source[ptr] == ':')
            {
                token = Token.TOKENIZER_COLON;
            }
            else if(source[ptr] == ';') 
            {
                token = Token.TOKENIZER_SEMICOLON;
            }
            else if(source[ptr] == '+')
            {
                token = Token.TOKENIZER_PLUS;
            }
            else if(source[ptr] == '-')
            {
                token = Token.TOKENIZER_MINUS;
            }
            else if(source[ptr] == '&')
            {
                token = Token.TOKENIZER_AMPERSAND;
            }
            else if(source[ptr] == '|')
            {
                token = Token.TOKENIZER_BAR;
            }
            else if(source[ptr] == '*')
            {
                token = Token.TOKENIZER_ASTR;
            }
            else if(source[ptr] == '/')
            {
                token = Token.TOKENIZER_SLASH;
            }
            else if(source[ptr] == '%')
            {
                token = Token.TOKENIZER_MOD;
            }
            else if(source[ptr] == '(')
            {
                token = Token.TOKENIZER_LEFTPAREN;
            }
            else if(source[ptr] == '#')
            {
                token = Token.TOKENIZER_HASH;
            }
            else if(source[ptr] == ')')
            {
                token = Token.TOKENIZER_RIGHTPAREN;
            }
            else if(source[ptr] == '<')
            {
                token = Token.TOKENIZER_LT;
            }
            else if(source[ptr] == '>')
            {
                token = Token.TOKENIZER_GT;
            }
            else if (source[ptr] == '$')
            {
                token = Token.TOKENIZER_DOLLAR;
            }
            else if (source[ptr] == '^')
            {
                token = Token.TOKENIZER_EXPONENT;
            }
            else if(source[ptr] == '=')
            {
                token = Token.TOKENIZER_EQ;
            }
            Debug.WriteLine("Out CheckSingleChar()");
            return (token);
        }

        public Token GetNextToken()
        {
            Debug.WriteLine("In GetNextToken()");

            Token token = Token.TOKENIZER_NONE;
            int i;
            TraceInternal.TraceVerbose("GetNextToken():" + Convert.ToString(ptr));

            if ((ptr == source.Length) || (source[ptr] == (char)0))
            {
                token = Token.TOKENIZER_ENDOFINPUT;
            }
            else
            {
                // Need to separate integer from numeric
                // .5 is valid

                if (IsNumber(source[ptr]))
                {
                    token = Token.TOKENIZER_INTEGER;

                    for (i = 0; i < MaximumNumberLength; ++i)
                    {
                        if (source[ptr + i] == '.')
                        {
                            token = Token.TOKENIZER_NUMBER;
                        }
                        else if (!IsDigit(source[ptr + i]))
                        {
                            if (i > 0)
                            {
                                nextptr = ptr + i;
                                break;
                            }
                            else
                            {
                                TraceInternal.TraceVerbose("get_next_token: error due to too short number");
                                token = Token.TOKENIZER_ERROR;
                                break;
                            }
                        }
                    }
                    if (i >= MaximumNumberLength)
                    {
                        TraceInternal.TraceVerbose("get_next_token: error due to too long number");
                        token = Token.TOKENIZER_ERROR;
                    }

                }
                else if (CheckSingleChar() != 0)
                {
                    nextptr = ptr + 1;
                    token = CheckSingleChar();
                }
                else if (source[ptr] == '"')
                {
                    nextptr = ptr;
                    do
                    {
                        ++nextptr;
                    }
                    while (source[nextptr] != '"');

                    ++nextptr;
                    token = Token.TOKENIZER_STRING;
                }
                else
                {
                    foreach (TokenKeyword keyword in keywords)
                    {
                        string c;
                        if (ptr + keyword.Keyword.Length > source.Length)
                        {
                            c = "";
                        }
                        else
                        {
                            c = new string(source, ptr, keyword.Keyword.Length);
                        }
                        // 2015/06/29 Allowed keywords to be uppercase
                        if ((keyword.Keyword == c) || (keyword.Keyword == c.ToLower()))
                        {
                            nextptr = ptr + keyword.Keyword.Length;
                            token = keyword.Token;
                            break;
                        }
                    }
                }

                // 2015/06/29 Allowed variables to be uppercase
                // 2015/07/04 Extend to allow for string variable names
                // 2015/10/08 Extend to allow for string array variable names

                // <varable> ::= <letter> | <letter> "$" |<letter> <digit> | <letter> <digit> "$" | <letter> "(" | <letter> "$" "("

                if (token == Token.TOKENIZER_NONE)
                {
                    if ((source[ptr] >= 'a' && source[ptr] <= 'z') || (source[ptr] >= 'A' && source[ptr] <= 'Z'))
                    {
                        nextptr = ptr + 1;
                        token = Token.TOKENIZER_NUMERIC_VARIABLE;

                        if (IsDigit(source[nextptr]))
                        {
                            // Two digit variable
                            nextptr++;
                            token = Token.TOKENIZER_NUMERIC_VARIABLE;

                            if (source[nextptr] == '$')
                            {
                                // String viarable
                                nextptr++;
                                token = Token.TOKENIZER_STRING_VARIABLE;

                                if (source[nextptr] == '(')
                                {
                                    // String array variable
                                    nextptr++;
                                    token = Token.TOKENIZER_STRING_ARRAY_VARIABLE;
                                }
                            }
                            else if (source[nextptr] == '(')
                            {
                                // Array variable
                                nextptr++;
                                token = Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE;
                            }
                        }
                        else
                        {
                            if (source[nextptr] == '$')
                            {
                                // String viarable
                                nextptr++;
                                token = Token.TOKENIZER_STRING_VARIABLE;

                                if (source[nextptr] == '(')
                                {
                                    // String array variable
                                    nextptr++;
                                    token = Token.TOKENIZER_STRING_ARRAY_VARIABLE;
                                }
                            }
                            else if (source[nextptr] == '(')
                            {
                                // Array variable
                                nextptr++;
                                token = Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE;
                            }
                        }
                    }
                }
            }
            Debug.WriteLine("Out GetNextToken()");
            return (token);
        }

        public void GotoPosition(int position)
        {
            Debug.WriteLine("In GotoPosition()");
            ptr = position;
            currentToken = GetNextToken();
            Debug.WriteLine("Out GotoPosition()");
        }
    
        public void Init(int position)
        {
            Debug.WriteLine("In Init()");
            GotoPosition(position);
            currentToken = GetNextToken();
            Debug.WriteLine("Out Init()");
        }

        public Token GetToken()
        {
            Debug.WriteLine("In GetToken()");
            return (currentToken);
        }

        public void NextToken()
        {
            Debug.WriteLine("In NextToken()");
            if (!IsFinished())
            {
                TraceInternal.TraceVerbose("NextToken: pointer=" + Convert.ToString(ptr) + " token=" + Convert.ToString(currentToken));
                ptr = nextptr;

                while (source[ptr] == ' ')
                {
                    ++ptr;
                }
                currentToken = GetNextToken();

                TraceInternal.TraceVerbose("NextToken: pointer=" + Convert.ToString(ptr) + " token=" + Convert.ToString(currentToken));
            }
            else
            {
                currentToken = Token.TOKENIZER_ENDOFINPUT;
            }
            Debug.WriteLine("Out NextToken()");
        }

        public void SkipTokens()
        {
            Debug.WriteLine("Out SkipTokens()");
            if (!IsFinished())
            {
                while (!(IsFinished() || source[nextptr] == '\n'))
                {
                    ++nextptr;
                }
                if (source[nextptr] == '\n')
                {
                    NextToken();
                }
            }

            TraceInternal.TraceVerbose("SkipTokens: " + Convert.ToString(ptr) + " " + Convert.ToString(currentToken));
            
            Debug.WriteLine("Out SkipTokens()");
        }

        public int GetInteger()
        {
            Debug.WriteLine("In GetInteger()");
            int integer= 0;
            int i = ptr;
            while (IsDigit(source[i]))
            {
                integer = 10 * integer + Convert.ToInt16(source[i]) - Convert.ToInt16('0');
                i++;
            }
            Debug.WriteLine("Out GetInteger()");
            return (integer);
        }

        public double GetNumber()
        {
            Debug.WriteLine("In GetNumber()");
            double number = 0;
            int i = ptr;
            int j = ptr;
            bool integer = true;
             
            while (IsNumber(source[i]))
            {
                if (source[i] == '.')
                {
                    integer = false;
                    j = i;
                }
                else if (integer == true)
                {
                    number = 10 * number + (double)Convert.ToInt32(source[i]) - Convert.ToInt32('0');
                }
                else
                {
                    number += (double)(Convert.ToInt32(source[i]) - Convert.ToInt32('0')) / Math.Pow(10,i-j);
                }
                i++;
            }
            Debug.WriteLine("Out GetNumber()");
            return (number);
        }

        public string Getstring()
        {
            Debug.WriteLine("In Getstring()");
            string _string = "";
            int i = ptr;

            if(GetToken() != Token.TOKENIZER_STRING)
            {
                _string = "";
            }
            else
            {
                i++;
                while(source[i] != '\"')
                {
                    _string += source[i];
                    i++;
                }
            }
            Debug.WriteLine("Out Getstring()");
            return (_string);
        }

        public bool IsFinished()
        {
            Debug.WriteLine("In IsFinished()");
            return ((ptr >= source.Length) || (nextptr >= source.Length) || (currentToken == Token.TOKENIZER_ENDOFINPUT));
        }

        public int GetIntegerVariable()
        {
            Debug.WriteLine("Int GetIntegerVariable()");
            int integer;
            if ((source[ptr] >= 'a') && (source[ptr] < 'z'))
            {
                integer = (int)source[ptr] - (int)'a'; 
            }
            else
            {
                integer = (int)source[ptr] - (int)'A';
            }
            Debug.WriteLine("Out GetIntegerVariable()");
            return (integer);
        }

        public string GetNumericVariable()
        {
            Debug.WriteLine("Int GetNumericVariable()");
            string value = "";
            char c;
            c = source[ptr];
            if (((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
            {
                value += c.ToString().ToLower(); // Make variables case insentitive
                ptr++;
            }

            c = source[ptr];
            if ((c >= '0') && (c <= '9'))
            {
                value += c;
            }
            Debug.WriteLine("Out GetNumericVariable()");
            return (value);
        }

        public string GetNumericArrayVariable()
        {
            Debug.WriteLine("In GetNumericArrayVariable()");

            // Numeric array variables are single digit
            // This appears not to be the case with ALTAIR

            string value = "";
            char c;

            c = source[ptr];
            if (((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
            {
                value += c.ToString().ToLower(); // Make variables case insentitive
                ptr++;
            }

            c = source[ptr];
            if ((c >= '0') && (c <= '9'))
            {
                value += c;
            }

            Debug.WriteLine("Out GetNumericArrayVariable()");
            return (value);
        }

        public string GetStringArrayVariable()
        {
            Debug.WriteLine("In GetStringArrayVariable()");

            // String array variables are single digit
            // This appears not to be the case with ALTAIR

            string value = "";
            char c;

            c = source[ptr];
            if (((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
            {
                value += c.ToString().ToLower(); // Make variables case insentitive
                ptr++;
            }

            c = source[ptr];
            if ((c >= '0') && (c <= '9'))
            {
                value += c;
            }

            Debug.WriteLine("Out GetStringArrayVariable()");
            return (value);
        }

        public string GetStringVariable()
        {
            Debug.WriteLine("In GetStringVariable()");

            string value = "";
            char c;

            c = source[ptr];
            if (((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
            {
                value += c.ToString().ToLower(); // Make variables case insentitive
                ptr++;
            }

            c = source[ptr];
            if ((c >= '0') && (c <= '9'))
            {
                value += c;
            }

            Debug.WriteLine("Out GetStringVariable()");

            return (value);
        }

        public int GetPosition()
        {
            Debug.WriteLine("In GetPosition()");
            Debug.WriteLine("Out GetPosition()");
            return ptr;
        }

        #endregion
        #region Private

        //--------------------------------------------------------------
        // Recognize a Numeric Digit 

        private bool IsDigit(char check)
        {
            return (Char.IsDigit(check));
        }

        //--------------------------------------------------------------
        // Recognize a Number 

        private bool IsNumber(char check)
        {
            return (Char.IsDigit(check) || (check == '.'));
        }

        //--------------------------------------------------------------
        // Report What Was Expected

        private void Expected(string message)
        {
            throw new System.ArgumentException("Unacceptable", message);
        }

        #endregion
    }
}