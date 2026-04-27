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
using System.Collections.Generic;
using System.Collections;
using BasicLibrary;
using System.Diagnostics;
using TracerLibrary;

namespace Altair
{
    /// <summary>
    /// Altair basic 1975
    /// </summary>
    public class Interpreter : IInterpreter
    {
        #region Fields

        readonly IDefaultIO _IO;

        int program_ptr;
        const int MAX_STRINGLEN = 40;

        // Go-sub

        public struct Gosub
        {

            private int lineNumber;
            private int posAfterGosub;

            public Gosub(int lineNumber, int positionAfterGosub)
            {
                this.lineNumber = lineNumber;
                this.posAfterGosub = positionAfterGosub;
            }

            public int Line_number {get { return lineNumber; } set { lineNumber = value; } }
            public int Pos_after_gosub { get { return posAfterGosub; } set { posAfterGosub = value; } }

        }

        const int MAX_GOSUB_STACK_DEPTH = 10;
        readonly Gosub[] gosubStack = new Gosub[MAX_GOSUB_STACK_DEPTH];
        int gosubStackPointer;

        // for-next

        public struct ForState
        {
            private int positionAfterFor;
            private string forVariable;
            private double from;
            private double to;
            private double step;

            public ForState(int positionAfterFor, string forVariable, double from, double to, double step)
            {
                this.positionAfterFor = positionAfterFor;
                this.forVariable = forVariable;
                this.from = from;
                this.to = to;
                this.step = step;
            }

            public int PositionAfterFor { get { return positionAfterFor; } set { positionAfterFor = value; } }
            public string ForVariable { get { return forVariable; } set { forVariable = value; } }
            public double From { get { return from; } set { from = value; } }
            public double To { get { return to; } set { to = value; } }
            public double Step { get { return step; } set { step = value; } }
        }

        const int MAX_FOR_STACK_DEPTH = 4;
        readonly ForState[] forStack = new ForState[MAX_FOR_STACK_DEPTH];
        static int forStackPointer;

        // lines

        private struct LineIndex
        {
            private readonly int lineNumber;
            private readonly int programTextPosition;

            public LineIndex(int lineNumber, int programTextPosition)
            {
                this.lineNumber = lineNumber;
                this.programTextPosition = programTextPosition;
            }

            public int LineNumber { get { return lineNumber; } }
            public int ProgramTextPosition { get { return programTextPosition; } }
        }

        readonly List<LineIndex> lineIndex;

        // Data

        const int MAX_DATA = 10;
        readonly Queue data = new Queue(MAX_DATA);
        int dataPos = 0;

        bool ended;

        private readonly char[] program;

        private readonly Tokenizer tokenizer;
        private readonly Evaluator evaluator;

        int nested = 0;
        int currentLineNumber = 0;

        #endregion
        #region Constructors

        public Interpreter(char[] program, IDefaultIO consoleIO)
        {
            this._IO = consoleIO;        
            lineIndex = new List<LineIndex>();
            tokenizer = new Tokenizer(program);
            evaluator = new Evaluator(tokenizer);
            this.program = program;
        }

        #endregion Constructors
        #region Methods

        public void Init(int position)
        {
            Debug.WriteLine("In Init()");
            program_ptr = position;
            forStackPointer = 0;
            gosubStackPointer = 0;
            IndexFree();
            tokenizer.Init(position);
            ended = false;
            Debug.WriteLine("Out Init()");
        }

        /// <summary>
        /// Run()
        /// </summary>
        public void Run()
        {
            Debug.WriteLine("In Run()");
            if (tokenizer.IsFinished())
            {
                TraceInternal.TraceVerbose("Program finished");
                return;
            }
            LineStatement();
            Debug.WriteLine("Out Run()");
        }

        public void Stop()
        {
            Debug.WriteLine("In Stop()");
            ended = true;
            Debug.WriteLine("Out Stop()");
        }

        /// <summary>
        /// Raise exception
        /// </summary>
        /// <param name="text"></param>
        public void Abort(string text)
        {
            string message = text + " @ " + currentLineNumber;
            throw new Exception(message);
        }

        /// <summary>
        /// Finished()
        /// </summary>
        /// <returns></returns>
        public bool IsFinished()
        {
            Debug.WriteLine("In Finished()");
            bool finished = ended || tokenizer.IsFinished();
            Debug.WriteLine("Out Finished()");
            return (finished);
        }

        #endregion
        #region Private

        private void IndexFree()
        {
            Debug.WriteLine("In IndexFree()");
            lineIndex.Clear();
            Debug.WriteLine("Out IndexFree()");
        }

        private int IndexFind(int lineNumber)
        {
            Debug.WriteLine("In IndexFind()");
            int line = 0;
            LineIndex idx = lineIndex.Find(x => x.LineNumber == lineNumber);
            if (idx.LineNumber == 0)
            {
                TraceInternal.TraceVerbose("IndexFind: Returning zero for " + lineNumber);
                line = 0;
            }
            else
            {
                TraceInternal.TraceVerbose("IndexFind: Returning index for line " + Convert.ToString(lineNumber));
                line = idx.ProgramTextPosition;
            }
            Debug.WriteLine("Out IndexFind()");
            return (line);
        }

        private void IndexAdd(int lineNumber, int sourcePosition)
        {
            Debug.WriteLine("In IndexAdd()");
            LineIndex idx = new LineIndex(lineNumber, sourcePosition);
            lineIndex.Add(idx);
            TraceInternal.TraceVerbose("IndexAdd: Adding index for line " + Convert.ToString(lineNumber) + " @ " + Convert.ToString(sourcePosition));
            Debug.WriteLine("Out IndexAdd()");
        }

        private void JumpLineNumberSlow(int lineNumber)
        {
            Debug.WriteLine("In JumpLineNumberSlow()");
            tokenizer.Init(program_ptr);

            while (tokenizer.GetInteger() != lineNumber)
            {
                do
                {
                    do
                    {
                        //Tokenizer_next();
                        tokenizer.SkipTokens();
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR && tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT);

                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                    {
                        tokenizer.NextToken();
                    }

                }
                while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_INTEGER);
                TraceInternal.TraceVerbose("JumpLineNumber_slow: Found line " + tokenizer.GetInteger());
            }
            Debug.WriteLine("Out JumpLineNumberSlow()");
        }

        /// <summary>
        /// Jump to a specific line number
        /// </summary>
        /// <param name="lineNumber"></param>
        private void JumpLineNumber(int lineNumber)
        {
            Debug.WriteLine("In JumpLineNumber()");
            int pos = IndexFind(lineNumber);
            if (pos > 0)
            {
                TraceInternal.TraceVerbose("JumpLineNumber: Going to line " + lineNumber);
                tokenizer.GotoPosition(pos);
            }
            else
            {
                // We'll try to find a yet un-indexed line to jump to.
                TraceInternal.TraceVerbose("JumpLineNumber: Calling JumpLineNumber_slow " + lineNumber);
                JumpLineNumberSlow(lineNumber);
            }
            currentLineNumber = lineNumber;
            Debug.WriteLine("Out JumpLineNumber()");
        }

        private string ReadInput()
        {
            Debug.WriteLine("In ReadInput()");
            string value = _IO.In();
            value = value.TrimEnd('\r');
            value = value.TrimEnd('\n');
            Debug.WriteLine("In ReadInput()");
            return (value);
        }

        private bool ReadData(int position)
        {
            Debug.WriteLine("In ReadData()");
            bool read = false;
            int current_pos = tokenizer.GetPosition();
            bool negative = false;

            tokenizer.Init(position);
            do
            {
                do
                {
                    tokenizer.NextToken();
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_REM)
                    {
                        RemStatement();
                    }
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_DATA) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.IsFinished() == false));

                TraceInternal.TraceVerbose("ReadData: Read");
				TraceInternal.TraceInformation("READ");

                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_DATA)
                {
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DATA);
                    // now read all the data

                    do
                    {
                        double number;
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            negative = false;
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMBER)
                        {
                            number = tokenizer.GetNumber();
                            if (negative == true)
                            {
                                TraceInternal.TraceVerbose("ReadData: add number " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                TraceInternal.TraceVerbose("ReadData: add number " + number);
                                data.Enqueue(number);
                                read = true;
                            }
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                        {
                            number = tokenizer.GetNumber();
                            if (negative == true)
                            {
                                TraceInternal.TraceVerbose("ReadData: add integer " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                TraceInternal.TraceVerbose("ReadData: add integer " + number);
                                data.Enqueue(number);
                                read = true;
                            }
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                        {
                            TraceInternal.TraceVerbose("ReadData: add string " + tokenizer.Getstring());
                            data.Enqueue(tokenizer.Getstring());
                            read = true;
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_MINUS)
                        {
                            negative = true;
                            tokenizer.NextToken();
                        }
                        else
                        {
                            negative = false;
                            tokenizer.NextToken();
                        }
                    }
                    while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) && (tokenizer.IsFinished() == false));

                    dataPos = tokenizer.GetPosition();
                    TraceInternal.TraceVerbose("ReadData: Found data");
                }
            }
            while ((read == false) && (tokenizer.IsFinished() == false));
            tokenizer.Init(current_pos);
            Debug.WriteLine("Out ReadData()");
            return (read);
        }

        /// <summary>
        /// Output data
        /// </summary>
        /// <param name="s"></param>
        private void Emit(string s)
        {
            _IO.Out(s);
        }

        private string FormatNumber(Double number)
        {
            Debug.WriteLine("In FormatNumber()");
            char sign = ' ';


            // a number may contain up to 9 digits excluding the decimal point, and 1 digit for sign (+ve is space)
            // , so 11 total
            // it appears that the leading zero of a number is removed 
            //
            // need to format the numeric string
            // {sign}{integer} <= 11 digits
            // {sign}{integer}{.}{decimal} <= 11 digits
            // {sign}{integer}{.}{decimal}{E}{sign}{exponent}
            //

            string value;
            // check if +/- integer

            if (Math.Truncate(number) == number)
            {
                value = Convert.ToString(number);
                if (value.Substring(0, 1) == "-")
                {
                    sign = '-';
                    value = value.Substring(1);     // remove the sign
                }
                if (value.Length > 9)
                {
                    value = Math.Abs(number).ToString("0.##### E+0");
                }
                value = sign + value;
            }
            else
            {
                value = Convert.ToString(number);
                if (value.Substring(0, 1) == "-")
                {
                    sign = '-';
                }
                if ((Math.Abs(number) < 0.1) && (value.Length > 6))
                {
                    value = Math.Abs(number).ToString("0.##### E+0");
                }
                else
                {
                    if (Math.Abs(number) < 1)
                    {
                        value = Math.Abs(number).ToString(".######");
                    }
                    else
                    {
                        value = Math.Abs(number).ToString("0.#####");
                    }
                }
                value = sign + value;
            }
            Debug.WriteLine("Out FormatNumber()");
            return (value);
        }

        #region Statements

        // <program> ::= <line> [ <line> ]*;
        // <line> ::= <line_number> <statement> [ <statement_separator> <statement> ]* <eol>;
        // <line_number> ::= <number> [ <zero> | <number> ]*;
        // <number> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
        // <zero> ::= "0";
        // <statement_separator> ::= ":"
        // <eol> ::= <cr> <lf> | <cr>;
        // <statement> ::= <OnStatement> | <IfStatement> | <DimStatement> | <LetStatement> 
        // <OnStatement> ::= "ON" <expression> "THEN" , <line> [ "," <line> ]* | "ON" <expression> "GOTO" <line> [ "," <line> ]*
        // <IfStatement> ::= "IF" <expression> <relation> <expression> "THEN" <line> | "IF" <expression><relation><expression> "GOTO" <line>
        // <DimStatement> ::= "DIM" <variable> "(" <expression> ")"
        // <LetStatement> ::= "LET" <variable> "=" <expression>
        // <RandomizeStatement> :== "RANDOMIZE"
        // <DataStatement> := "DATA" <data> ["," <data>]*
        // <RestoreStatement> ::= "RESTORE" 
        // <GotoStatement> ::= "GOTO" <line_number>
        // <PrintStatement> ::=
        // <LetStatement> ::= "LET" <variable> "=" <expression> ["," <variable> "=" <expression>]*
        // <variable> ::= <StringVariable> | <NumericVariable> | <IntegerVariable> | 


        /// <summary>
        /// LineStatement
        /// </summary>
        private void LineStatement()
        {
            Debug.WriteLine("In LineStatement()");
            currentLineNumber = tokenizer.GetInteger();
            TraceInternal.TraceInformation("----------- Line number " + currentLineNumber + " ---------");
            IndexAdd(currentLineNumber, tokenizer.GetPosition());
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            Statement();
            Debug.WriteLine("Out LineStatement()");
        }

        /// <summary>
        /// Statement
        /// </summary>
        private bool Statement()
        {
            bool inline = false;
            nested++;
            Tokenizer.Token token;

            Debug.WriteLine("In Statement()");

            // Might need to consider a loop here for multi-line statements
            // otherwise it will error saying the line number is missing.

            do
            {
                token = tokenizer.GetToken();

                switch (token)
                {
                    case Tokenizer.Token.TOKENIZER_INPUT:
                        {
                            InputStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DATA:
                        {
                            DataStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RESTORE:
                        {
                            RestoreStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_PRINT:
                        {
                            PrintStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_IF:
                        {
                            inline = IfStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOTO:
                        {
                            inline = GotoStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOSUB:
                        {
                            inline = GosubStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RETURN:
                        {
                            inline = ReturnStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_FOR:
                        {
                            ForStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_NEXT:
                        {
                            NextStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_END:
                        {
                            EndStatement();
                            break;
                        }
					case Tokenizer.Token.TOKENIZER_STOP:
                        {
                            StopStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_LET:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LET);
                            LetStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_REM:
                        {
                            RemStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DIM:
                        {
                            DimStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_READ:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_READ);
                            ReadStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DEF:
                        {
                            DefStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_ON:
                        {
                            inline = OnStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RANDOMIZE:
                        {
                            RandomizeStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_STRING_VARIABLE:
                    case Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE:
                    case Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE:
                    case Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE:
                        {
                            LetStatement();
                            break;
                        }
                    default:
                        {
                            Abort("Statement: Not implemented " + token);
                            break;
                        }
                }

                TraceInternal.TraceVerbose("statement: " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR) + " " + inline);

                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
                {
                    tokenizer.NextToken();
                    inline = true;
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_ENDOFINPUT))
                {
                    TraceInternal.TraceVerbose("statement: nested=" + nested);
                    if (nested == 1)
                    {
                        tokenizer.NextToken();
                    }
                    inline = false;
                }
            }
            while ((inline == true) && (tokenizer.IsFinished() == false));
            nested--;

            Debug.WriteLine("Out Statement()");
            return (inline);
        }

        /// <summary>
        /// RANDOMIZE
        /// </summary>
        private void RandomizeStatement()
        {
            Debug.WriteLine("In RandomizeStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RANDOMIZE);
            evaluator.Randomize();
            Debug.WriteLine("Out RandomizeStatement()");
        }

        /// <summary>
        /// DATA
        /// </summary>
        private void DataStatement()
        {
           Debug.WriteLine("In In DataStatement()");
            tokenizer.SkipTokens();
           Debug.WriteLine("Out DataStatement()");
        }

        /// <summary>
        /// RESTORE
        /// </summary>
        private void RestoreStatement()
        {
           Debug.WriteLine("In RestoreStatement()");
           tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RESTORE);
           dataPos = 0;
           Debug.WriteLine("Out RestoreStatement()");
        }

        /// <summary>
        /// GOTO
        /// </summary>
        private bool GotoStatement()
        {
            Debug.WriteLine("In GotoStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
            int lineNumber = tokenizer.GetInteger();
            TraceInternal.TraceInformation("GOTO " + lineNumber);
            JumpLineNumber(lineNumber);
            Debug.WriteLine("Out GotoStatement()");
            return (false);
        }

        /// <summary>
        /// PRINT
        /// </summary>
        private void PrintStatement()
        {
            Tokenizer.Token previous = Tokenizer.Token.TOKENIZER_NULL;

            Debug.WriteLine("In PrintStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_PRINT);
            // Print
            // PRINT "HELLO"{CR}
            // PRINT "A=";A{CR}
            // PRINT "HELLO":LET A=5{CR}
            // PRINT "A=";A:LET A=5{CR}
            // PRINT TAB(10);"A=";A{CR}

            TraceInternal.TraceInformation("PRINT");

            char control;
            do
            {
                TraceInternal.TraceVerbose("PrintStatement: Print loop");
                int tab;
                double number;
                string value;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                {
                    previous = tokenizer.GetToken();
                    value = tokenizer.Getstring();
                    TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                    Emit(value);
                    tokenizer.NextToken();
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    // assume a tab spacing of 15 characters
                    // spec defines 5 zones then new line

                    previous = tokenizer.GetToken();
                    tab = -_IO.CursorLeft + _IO.Zone * (1 + (_IO.CursorLeft / _IO.Zone));
                    value = new string(' ', tab);
                    TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                    Emit(value);
                    control = ',';
                    tokenizer.NextToken();

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_SEMICOLON)
                {
                    if ((previous == Tokenizer.Token.TOKENIZER_SEMICOLON) || (previous == Tokenizer.Token.TOKENIZER_STRING) || (previous == Tokenizer.Token.TOKENIZER_STRING_VARIABLE) || (previous == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE))
                    {
                        // additional rule appears to be that if the ';' follows text then it concatenates
                        // if ';' follows a number then it move tab zones.
                    }
                    else
                    {
                        // assume a tab spacing of 3 characters
                        // spec defines a minimum of 6 characters

                        tab = -_IO.CursorLeft + _IO.Compact * (1 + (_IO.CursorLeft / _IO.Compact));
                        if (tab < 2)
                        {
                            tab += 3;
                        }
                        tab = 1;
                        value = new string(' ', tab);
                        TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                        Emit(value);
                    }
                    previous = tokenizer.GetToken();
                    control = ';';
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
                {
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMBER) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER))
                {
                    previous = tokenizer.GetToken();
                    number = tokenizer.GetNumber();
                    value = FormatNumber(number);
                    TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                    Emit(value);
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE))
                {
                    previous = tokenizer.GetToken();
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    value = FormatNumber(number);
                    TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                    Emit(value);
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE))
                {
                    previous = tokenizer.GetToken();
                    evaluator.Expression();
                    value = evaluator.PopString();
                    TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                    Emit(value);
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_TAB)
                {
                    previous = tokenizer.GetToken();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAB);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                    evaluator.Expression();
                    tab = (int)Math.Truncate(evaluator.PopDouble());
                    if (tab > 0)
                    {
                        // Error here as TAB is not spaces but absolute so
                        // might be issues if the cursor position is beyond the 
                        // the current tab

                        if (_IO.CursorLeft < tab)
                        {
                            tab = tab - _IO.CursorLeft - 1;
                        }
                        else
                        {
                            Emit("\n");
                        }

                        value = new string(' ', tab);
                        TraceInternal.TraceInformation("PRINT TAB(" + tab + ")");
                        Emit(value);
                    }
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    control = '\n';
                }
                else
                {
                    evaluator.Expression();

                    // How do we know what to pop off the stack.

                    object data = evaluator.PopObject();
                    if (data.GetType() == typeof(int))
                    {
                        int integer = Convert.ToInt32(data);
                        value = FormatNumber(integer);
                        TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                        previous = Tokenizer.Token.TOKENIZER_INTEGER;
                        Emit(value);
                    }
                    else if (data.GetType() == typeof(string))
                    {
                        value = data.ToString();
                        TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                        Emit(value);
                        previous = Tokenizer.Token.TOKENIZER_STRING;
                    }
                    else if (data.GetType() == typeof(double))
                    {
                        number = Convert.ToDouble(data);
                        value = FormatNumber(number);
                        TraceInternal.TraceInformation("PRINT \"" + value + "\"");
                        Emit(value);
                        previous = Tokenizer.Token.TOKENIZER_NUMBER;
                    }
                    control = '\n';
                }

                TraceInternal.TraceVerbose("PrintStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) && (tokenizer.IsFinished() == false));

            if (control == '\n')
            {
                Emit("\n");
            }

            Debug.WriteLine("Out PrintStatement()");
        }

        /// <summary>
        /// IF
        /// </summary>
        /// <returns></returns>
        private bool IfStatement()
        {
            // The if statement 
            // IF A=1 THEN B=3{CR}
            // IF A=1 PRINT "a=";a{CR}
            // IF A=1 THEN 20{CR}
            // IF A=1 THEN B=3{COLON}LET C=4
            // IF A=1 PRINT "a=";a{COLON}GOTO 20{CR}

            bool jump = true;
            Debug.WriteLine("In IfStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_IF);
            TraceInternal.TraceInformation("IF");
            evaluator.BinaryExpression();

            Tokenizer.Token token = tokenizer.GetToken();

            int lineNumber;
            if (token == Tokenizer.Token.TOKENIZER_GOTO)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
                if (evaluator.PopBoolean() == true)
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                    {
                        lineNumber = tokenizer.GetInteger();
                        TraceInternal.TraceInformation("GOTO " + lineNumber);
                        JumpLineNumber(lineNumber);
                        jump = false;
                    }
                    //else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE))
                    //{
                    //    evaluator.Expression();
                    //}
                    else
                    {
                        jump = Statement();
                    }
                }
                else
                {
                    do
                    {
                        tokenizer.NextToken();
                    }
                    // Wonder if this should include separator
                    while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
                }
            }
            else if (token == Tokenizer.Token.TOKENIZER_THEN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
                if (evaluator.PopBoolean() == true)
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                    {
                        lineNumber = tokenizer.GetInteger();
                        TraceInternal.TraceInformation("THEN " + lineNumber);
                        JumpLineNumber(lineNumber);
                        jump = false;
                    }
                    //else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE))
                    //{
                    //    evaluator.Expression();
                    //}
                    else
                    {
                        jump = Statement();
                    }
                }
                else
                {
                    do
                    {
                        tokenizer.NextToken();
                    }
				    // Wonder if this should include separator
                    while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
                }
            }
            else
            {
                Abort("IfStatement: Not implemented " + token);
            }
            Debug.WriteLine("Out IfStatement()");
            return (jump);
        }

        /// <summary>
        /// ON
        /// </summary>
        private bool OnStatement()
        {
            // The on statement
            //
            // <statement> ::= <OnStatement> | <IfStatement>
            //
            // <OnStatement> ::= "ON" <expression> "THEN" <line> [ "," <line> ]*
            // <OnStatement> ::= "ON" <expression> "GOTO" <line> [ "," <line> ]*
            // <OnStatement> ::= "ON" <expression> "GOSUB" <line> [ "," <line> ]*
            // 
            // If expression > number of elements then goes to next statement.
            //

            bool jump = true;

            double number;
            int integer;
            int parameter = 0;
            bool check = true;
            int mode = -1;
            int lineNumber = 0;

            Debug.WriteLine("In OnStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ON);
            evaluator.Expression();
            number = evaluator.PopDouble();
            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_THEN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
                mode = 0;
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_GOTO)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
                mode = 1;
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_GOSUB)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOSUB);
                mode = 2;
            }

            integer = (int)Math.Truncate(number);

            if (integer < 1)
            {
                Abort("OnStstement: Expected: > 1");
            }
            else if (integer > 255)
            {
                Abort("OnStstement: Expected: < 256");
            }
            else
            {
                do
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                    {
                        parameter++;
                        if (integer == parameter)
                        {
                            lineNumber = tokenizer.GetInteger();
                        }
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
                    }
                    else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                    {
                        tokenizer.NextToken();
                    }
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));

                if (lineNumber > 0)
                {
                    if (mode == 0) // Then
                    {
                        TraceInternal.TraceVerbose("OnStatement: " + lineNumber);
                        TraceInternal.TraceInformation("ON " + integer + " THEN " + lineNumber);
                        JumpLineNumber(lineNumber);
                        jump = false;
                    }
                    else if (mode == 1)
                    {
                        TraceInternal.TraceVerbose("OnStatement: " + lineNumber);
                        TraceInternal.TraceInformation("ON " + integer + " GOTO " + lineNumber);
                        JumpLineNumber(lineNumber);
                        jump = false;
                    }
                    else if (mode == 2)
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CR);  // this is probematic

                            if (gosubStackPointer < MAX_GOSUB_STACK_DEPTH)
                            {
                                gosubStack[gosubStackPointer].Line_number = tokenizer.GetInteger();
                                gosubStack[gosubStackPointer].Pos_after_gosub = 0;
                                gosubStackPointer++;
                                TraceInternal.TraceVerbose("OnStatement: " + lineNumber);
                                TraceInternal.TraceInformation("ON " + integer + " GOSUB " + lineNumber);
                                JumpLineNumber(lineNumber);
                                jump = false;
                            }
                            else
                            {
                                Abort("GosubStatement: gosub stack exhausted");
                            }
                        }
                        // Statement of followed by another statement
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COLON);  // this is probematic

                            if (gosubStackPointer < MAX_GOSUB_STACK_DEPTH)
                            {
                                gosubStack[gosubStackPointer].Line_number = currentLineNumber;
                                gosubStack[gosubStackPointer].Pos_after_gosub = tokenizer.GetPosition();
                                gosubStackPointer++;
                                TraceInternal.TraceVerbose("OnStatement: " + lineNumber + "," + tokenizer.GetPosition());
                                TraceInternal.TraceInformation("ON " + integer + " GOSUB " + lineNumber + "," + tokenizer.GetPosition());
                                JumpLineNumber(lineNumber);
                                jump = false;
                            }
                            else
                            {
                                Abort("GosubStatement: gosub stack exhausted");
                            }
                        }
                    }
                }
            }

            Debug.WriteLine("Out OnStatement()");
            return (jump);
        }

        /// <summary>
        /// LET
        /// </summary>
        private void LetStatement()
        {
            string varName;
            int integer;
            double number;
            string value;
            int positions = 0;

            // The let statement can loop through a series of comma separated values and finish at a terminator
            // LET A=1{COMMA}B=2{CR}
            // LET A=1{COMMA}B=2:

            Debug.WriteLine("In LetStatement()");
            //tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LET);
            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    evaluator.SetNumericVariable(varName, number);
                    TraceInternal.TraceVerbose("LetStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                    TraceInternal.TraceInformation("LET " + Convert.ToString(varName) + "=" + number);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    value = evaluator.PopString();
                    evaluator.SetStringVariable(varName, value);
                    TraceInternal.TraceVerbose("LetStatement: assign '" + value + "' to string variable " + Convert.ToString(varName) + "$");
                    TraceInternal.TraceInformation("LET " + Convert.ToString(varName) + "$=\"" + value + "\"");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            positions++;
                            dimension[positions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    try
                    {
                        evaluator.SetNumericArrayVariable(varName, positions, dimension, number);
                    }
                    catch(Exception e)
                    {
                        TraceInternal.TraceError(e.ToString());
                    }
                    TraceInternal.TraceVerbose("LetStatement: assign " + number + " to numeric array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("LET " + Convert.ToString(varName) + "()" + "=" + number);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    int[] position = new int[10]; // 10 dimensional array limit !!!
                    position[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            positions++;
                            position[positions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    value = evaluator.PopString();
                    evaluator.SetStringArrayVariable(varName, positions, position, value);
                    TraceInternal.TraceVerbose("LetStatement: assign '" + value + "' to string array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("LET " + Convert.ToString(varName) + "$()" + "=\"" + value + "\"");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                TraceInternal.TraceVerbose("LetStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug.WriteLine("Out LetStatement()");
        }

        /// <summary>
        /// INPUT
        /// </summary>
        private void InputStatement()
        {
            string varName;
            int integer;
            double numeric;
            string value;
            int dimension = 0;
            int[] dimensions = new int[10]; // 10 dimensional array limit !!!
            string buffer = "";
            int buf_pointer = 0;

            // The input statement can loop through a series of comma separated values and finish at a terminator
            // INPUT A{COMMA}B{CR}
            // INPUT A{COMMA}B:
            // the input looks like it can also take a number of comma separated values and split out in 1 entry
            // ? 10,10 will read in the two values and assign to A and B.
            // this means I need an input buffer that can be parsed.
            // INPUT "QUESTION";A{COMMA}B{CR}
            // INPUT "QUESTION";A{COMMA}B:

            Debug.WriteLine("In InputStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INPUT);

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
            {
                Emit(tokenizer.Getstring());
                tokenizer.NextToken();
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SEMICOLON);
            }

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("? ");
                        buf_pointer = 0;
                        buffer = ReadInput();
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    // conversion could error here when we get to alphanumeric inputs
                    if (double.TryParse(value, out numeric) == false)
                    {
                        TraceInternal.TraceError("Expected: Double");
                    }
                    else
                    {
                        evaluator.SetNumericVariable(varName, numeric);
                    }
                    TraceInternal.TraceVerbose("InputStatement: assign " + numeric + " to numeric variable " + Convert.ToString(varName));
                    TraceInternal.TraceInformation("INPUT " + Convert.ToString(varName) + "=" + numeric);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("? ");
                        buf_pointer = 0;
                        buffer = ReadInput();
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    evaluator.SetStringVariable(varName, value);
                    TraceInternal.TraceVerbose("InputStatement: assign " + value + " to string variable " + Convert.ToString(varName) + "$");
                    TraceInternal.TraceInformation("INPUT " + Convert.ToString(varName) + "$=" + value);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    dimensions[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimension++;
                            dimensions[dimension] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    // wonder what the original did here if a number is not entered?
                    // looks like it exits out back to the command shell

                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    // conversion could error here when we get to alphanumeric inputs
                    if (!double.TryParse(value, out numeric))
                    {
                        TraceInternal.TraceError("Expected: Double");
                    }
                    evaluator.SetNumericArrayVariable(varName, dimension, dimensions, numeric);
                    TraceInternal.TraceVerbose("InputStatement: assign " + numeric + " to array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("INPUT " + Convert.ToString(varName) + "()=" + numeric);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    dimensions[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimension++;
                            dimensions[dimension] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    // wonder what the original did here if a number is not entered?
                    // looks like it exits out back to the command shell

                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    evaluator.SetStringArrayVariable(varName, dimension, dimensions, value);
                    TraceInternal.TraceVerbose("InputStatement: assign " + value + " to string array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("INPUT " + Convert.ToString(varName) + "()=" + value);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                TraceInternal.TraceVerbose("InputStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug.WriteLine("Out InputStatement()");
        }

        /// <summary>
        /// DIM
        /// </summary>
        private void DimStatement()
        {
            int numeric;

            // The dim statement can loop through a series of comma separated declarations
            // DIM A(1,1),B(1,2)....{CR}

            Debug.WriteLine("In DimStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DIM);
            do
            {
                string varName;
                int dimensions;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    dimensions = 0;
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            numeric = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = numeric;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    evaluator.DeclareNumericArrayVariable(varName, dimensions, dimension);
                    TraceInternal.TraceVerbose("DimStatement: declare numeric array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");
                    TraceInternal.TraceInformation("DIM " + Convert.ToString(varName) + "(" + Convert.ToString(dimensions) + ")");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    dimensions = 0;
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            numeric = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = numeric;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    evaluator.DeclareStringArrayVariable(varName, dimensions, dimension);
                    TraceInternal.TraceVerbose("DimStatement: declare string array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");
                    TraceInternal.TraceInformation("DIM " + Convert.ToString(varName) + "(" + Convert.ToString(dimensions) + ")");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    // Skip
                }
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON)) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));

            Debug.WriteLine("Out DimStatement()");
        }

        /// <summary>
        /// DEF
        /// </summary>
        private void DefStatement()
        {
            int parameters = 0;
            string[] parameter = new string[10]; // 10 parameter array limit !!!

            // The def statement is followed by the function reference and then an expression
            // DEF FN{function}({parameters})={expression}

            Debug.WriteLine("In DefStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DEF);
            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_FN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FN);
                string varName = tokenizer.GetNumericArrayVariable();
                int num = varName[0] - (int)'a';
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                do
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                    {
                        tokenizer.NextToken();
                    }
                    else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                    {
                        // this is the variable name
                        varName = tokenizer.GetNumericVariable();
                        parameter[parameters] = varName;
                        parameters++;
                        tokenizer.NextToken();
                    }
                    else
                    {
                        tokenizer.NextToken();
                    }
                }
                while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                evaluator.functions[num] = new Evaluator.FunctionIndex(tokenizer.GetPosition(), parameters, parameter);
                tokenizer.SkipTokens();
            }
            Debug.WriteLine("Out DefStatement()");
        }

        /// <summary>
        /// GOSUB
        /// </summary>
        private bool GosubStatement()
        {
            bool jump = false;
            int lineNumber;

            Debug.WriteLine("In GosubStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOSUB);
            lineNumber = tokenizer.GetInteger();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);

            // Last statement on the line
            if (tokenizer.GetNextToken() == Tokenizer.Token.TOKENIZER_CR)
            {

                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CR);  // this is problematic

                if (gosubStackPointer < MAX_GOSUB_STACK_DEPTH)
                {
                    gosubStack[gosubStackPointer].Line_number = tokenizer.GetInteger();
                    gosubStack[gosubStackPointer].Pos_after_gosub = 0;
                    gosubStackPointer++;
                    TraceInternal.TraceVerbose("GosubStatement: " + lineNumber);
                    TraceInternal.TraceInformation("GOSUB " + lineNumber);
                    JumpLineNumber(lineNumber);
                    jump = false;
                }
                else
                {
                    Abort("GosubStatement: gosub stack exhausted");
                }
            }
            // Statement of followed by another statement
            else if (tokenizer.GetNextToken() == Tokenizer.Token.TOKENIZER_COLON)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COLON);  // this is problematic

                if (gosubStackPointer < MAX_GOSUB_STACK_DEPTH)
                {
                    gosubStack[gosubStackPointer].Line_number = currentLineNumber;
                    gosubStack[gosubStackPointer].Pos_after_gosub = tokenizer.GetPosition();
                    gosubStackPointer++;
                    TraceInternal.TraceVerbose("GosubStatement: " + lineNumber + "," + tokenizer.GetPosition());
                    TraceInternal.TraceInformation("GOSUB " + lineNumber + "," + tokenizer.GetPosition());
                    JumpLineNumber(lineNumber);
                    jump = false;
                }
                else
                {
                    Abort("GosubStatement: gosub stack exhausted");
                }
            }
            Debug.WriteLine("Out GosubStatement()");
            return (jump);
        }

        /// <summary>
        /// RETURN
        /// </summary>
        private bool ReturnStatement()
        {
            bool jump = true;

            Debug.WriteLine("In ReturnStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RETURN);
            if (gosubStackPointer > 0)
            {
                gosubStackPointer--;
                if (gosubStack[gosubStackPointer].Pos_after_gosub > 0)
                {
                    // use the position to determine the multi-statement return point

                    TraceInternal.TraceVerbose("ReturnStatement: " + gosubStack[gosubStackPointer].Line_number + "," + gosubStack[gosubStackPointer].Pos_after_gosub);
                    TraceInternal.TraceInformation("RETURN " + gosubStack[gosubStackPointer].Line_number + "," + gosubStack[gosubStackPointer].Pos_after_gosub);
                    tokenizer.GotoPosition(gosubStack[gosubStackPointer].Pos_after_gosub);
                    jump = true;
                }
                else
                {
                    TraceInternal.TraceVerbose("ReturnStatement: " + gosubStack[gosubStackPointer].Line_number);
                    TraceInternal.TraceInformation("RETURN " + gosubStack[gosubStackPointer].Line_number);
                    JumpLineNumber(gosubStack[gosubStackPointer].Line_number);
                    jump = false;
                }
            }
            else
            {
                Abort("ReturnStatement: non-matching return");
            }
            Debug.WriteLine("Out ReturnStatement()");
            return (jump);
        }

        /// <summary>
        /// REM
        /// </summary>
        private void RemStatement()
        {
            Debug.WriteLine("In RemStatement");
            tokenizer.SkipTokens();
            Debug.WriteLine("Out RemStatement");
        }

        /// <summary>
        /// NEXT
        /// </summary>
        private void NextStatement()
        {
            double var;
            Debug.WriteLine("In NextStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NEXT);

            // Support compound next
            // NEXT I
            // NEXT I,J

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {

                    string varName = tokenizer.GetNumericVariable();
                    var = evaluator.PopDouble();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

                    TraceInternal.TraceVerbose("NextStatement: variable=" + varName + " value=" + var);
                    TraceInternal.TraceInformation("NEXT " + Convert.ToString(varName));

                    if (forStackPointer > 0 && varName == forStack[forStackPointer - 1].ForVariable)
                    {
                        // allow for negative steps
                        double step = forStack[forStackPointer - 1].Step;
                        evaluator.SetNumericVariable(varName, evaluator.GetNumericVariable(varName) + step);
                        if (step > 0)
                        {
                            if (evaluator.GetNumericVariable(varName) <= forStack[forStackPointer - 1].To)
                            {
                                tokenizer.GotoPosition(forStack[forStackPointer - 1].PositionAfterFor);
                            }
                            else
                            {
                                forStackPointer--;
                            }
                        }
                        else
                        {
                            if (evaluator.GetNumericVariable(varName) >= forStack[forStackPointer - 1].To)
                            {
                                tokenizer.GotoPosition(forStack[forStackPointer - 1].PositionAfterFor);
                            }
                            else
                            {
                                forStackPointer--;
                            }
                        }
                    }
                    else
                    {
                        TraceInternal.TraceError("non-matching next (expected " + forStack[forStackPointer - 1].ForVariable + ", found " + Convert.ToString(var) + ")\n");
                        Abort("NextStatement: Non-matching next " + Convert.ToString(var));
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                TraceInternal.TraceVerbose("NextStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug.WriteLine("Out NextStatement()");
        }

        /// <summary>
        /// FOR
        /// </summary>
        private void ForStatement()
        {
            double step = 1;

            Debug.WriteLine("In ForStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FOR);
            string varName = tokenizer.GetNumericVariable();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
            evaluator.Expression();
            double from = evaluator.PopDouble();
            evaluator.SetNumericVariable(varName, from);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TO);
            evaluator.Expression();
            double to = evaluator.PopDouble();

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STEP)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STEP);
                evaluator.Expression();
                step = evaluator.PopDouble();
            }

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
            {
                if (forStackPointer < MAX_FOR_STACK_DEPTH)
                {
                    // nasty effect where a goto bypasses the next.
                    // option is to check if the variable already exists and re-sue

                    for (int i=0; i < MAX_FOR_STACK_DEPTH; i++)
                    {
                        if (forStack[i].ForVariable == varName)
                        {
                            forStackPointer = i;
                            break;
                        }
                    }

                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].From = from;
                    forStack[forStackPointer].To = to;
                    forStack[forStackPointer].Step = step;
                    TraceInternal.TraceVerbose("ForStatement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    TraceInternal.TraceInformation("FOR " + Convert.ToString(varName) + "=" + from + " TO " + to + " STEP " + step);
                    forStackPointer++;
                }
                else
                {
                    TraceInternal.TraceError("ForStatement: for stack depth exceeded");
                }
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
            {
                if (forStackPointer < MAX_FOR_STACK_DEPTH)
                {

                    for (int i = 0; i < MAX_FOR_STACK_DEPTH; i++)
                    {
                        if (forStack[i].ForVariable == varName)
                        {
                            forStackPointer = i;
                            break;
                        }
                    }

                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].From = from;
                    forStack[forStackPointer].To = to;
                    forStack[forStackPointer].Step = step;
                    TraceInternal.TraceVerbose("ForStatement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    TraceInternal.TraceInformation("FOR " + Convert.ToString(varName) + "=" + from + " TO " + to + " STEP " + step);
                    forStackPointer++;
                }
                else
                {
                    TraceInternal.TraceError("ForStatement: for stack depth exceeded");
                }
            }
            Debug.WriteLine("Out ForStatement()");
        }

        /// <summary>
        /// END
        /// </summary>
        private void EndStatement()
        {
            Debug.WriteLine("In EndStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_END);
            TraceInternal.TraceInformation("END");
            ended = true;
            Debug.WriteLine("Out EndStatement()");
        }

        /// <summary>
        /// STOP
        /// </summary>
        private void StopStatement()
        {
            Debug.WriteLine("In StopStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STOP);
            TraceInternal.TraceInformation("STOP");
            ended = true;
            Debug.WriteLine("Out StopStatement()");
        }

        /// <summary>
        /// READ
        /// </summary>
        private void ReadStatement()
        {
            string varName;
            int integer;
            double number;
            string value;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The read statement can loop through a series of comma separated variables and finish at a terminator
            // READ A{COMMA}B{CR}
            // READ A{COMMA}B{COLON}

            Debug.WriteLine("In ReadStatement()");

            do
            {
                bool read;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("ReadStatement: Out of data");
                        }
                        else
                        {
                            number = Convert.ToDouble(data.Dequeue());
                            evaluator.SetNumericVariable(varName, number);
                            TraceInternal.TraceVerbose("ReadStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                            TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "=" + number);
                        }
                    }
                    else
                    {
                        number = Convert.ToDouble(data.Dequeue());
                        evaluator.SetNumericVariable(varName, number);
                        TraceInternal.TraceVerbose("ReadStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                        TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "=" + number);
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("ReadStatement: Out of data");
                        }
                        else
                        {
                            value = Convert.ToString(data.Dequeue());
                            evaluator.SetStringVariable(varName, value);
                            TraceInternal.TraceVerbose("ReadStatement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                            TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "$=" + value);
                        }
                    }
                    else
                    {
                        value = Convert.ToString(data.Dequeue());
                        evaluator.SetStringVariable(varName, value);
                        TraceInternal.TraceVerbose("ReadStatement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                        TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "$=" + value);
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("ReadStatement: Out of data");
                        }
                    }

                    varName = tokenizer.GetNumericArrayVariable();
                    dimension[0] = 1;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    number = Convert.ToDouble(data.Dequeue());
                    evaluator.SetNumericArrayVariable(varName, dimensions, dimension, number);
                    TraceInternal.TraceVerbose("ReadStatement: assign " + number + " to array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "()=" + number);

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("ReadStatement: Out of data");
                        }
                    }

                    varName = tokenizer.GetNumericArrayVariable();
                    dimension[0] = 1;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    value = Convert.ToString(data.Dequeue());
                    evaluator.SetStringArrayVariable(varName, dimensions, dimension, value);
                    TraceInternal.TraceVerbose("ReadStatement: assign " + value + " to array variable " + Convert.ToString(varName) + "(");
                    TraceInternal.TraceInformation("READ " + Convert.ToString(varName) + "()=" + value);

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                TraceInternal.TraceVerbose("ReadStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
           Debug.WriteLine("Out ReadStatement()");
        }

        #endregion Statements
        #endregion
    }
}