﻿using System;
using Compilers.Lab1.Constants;
using Compilers.Lab1.Models;

namespace Compilers.Lab1
{
	public class LexemProcessor
	{
		private string _buffer = "";
		private string _seekingBuffer = "";
		private int _pointer = 0;
		private LexemProcessorStates _state = LexemProcessorStates.Idle;
		private StringReader? _reader;
		private char[] _charBuffer = new char[1];
		private List<Lexem> _lexems = new List<Lexem>();
		private List<Variable> _variablesTable = new List<Variable>();
		private int _variablesCounter = 0;

		public LexemProcessor()
		{
			
		}

		public Tuple<IList<Lexem>, IList<Variable>> ProcessFile(string fileName)
		{
			using (_reader = new StringReader(File.ReadAllText(fileName)))
			{
				while (_state != LexemProcessorStates.Final)
				{
					switch (_state)
					{
						case LexemProcessorStates.Idle:
							{
								if (_reader.Peek() == -1)
								{
									_state = LexemProcessorStates.Final;
									break;
								}

								if (IsEmptyOrNextLine(_charBuffer[0]))
								{
									GetNextChar();
								}
								else if (char.IsLetter(_charBuffer[0]))
								{
									ClearBuffer();
									AddToBuffer(_charBuffer[0]);
									_state = LexemProcessorStates.ReadingIdentifier;
									GetNextChar();
								}
								else if (char.IsDigit(_charBuffer[0]))
								{
									ClearBuffer();
									AddToBuffer(_charBuffer[0]);
									_state = LexemProcessorStates.ReadingNum;
									GetNextChar();
								}
								else
								{
									_state = LexemProcessorStates.Delimeter;
									AddToBuffer(_charBuffer[0]);
									GetNextChar();
								}
								break;
							}
						case LexemProcessorStates.ReadingIdentifier:
							{
								if (char.IsLetterOrDigit(_charBuffer[0]))
								{
									AddToBuffer(_charBuffer[0]);
									GetNextChar();
								}
								else
								{
									var lexemRef = SearchInLexemDictionary();
									var typeRef = SearchInTypesDictionary();
									if (lexemRef.Item1 != -1)
									{
										AddLexem(LexemTypes.Identifier, lexemRef.Item1, lexemRef.Item2);
										ClearBuffer();
									}
									else if (typeRef.Item1 != -1) 
									{
										AddLexem(LexemTypes.DataType, typeRef.Item1, typeRef.Item2);
										ClearBuffer();
									}
									else
									{
										var variable = _variablesTable.Any(v => v.Name.Equals(_buffer));
										if (!variable)
										{
											var variableType = _lexems.LastOrDefault(c => c.Type == LexemTypes.DataType);
											if (variableType == null)
											{
												_state = LexemProcessorStates.Error;
												break;
											}
											_variablesTable.Add(new Variable(_variablesCounter++, variableType.Value, _buffer));
											AddLexem(LexemTypes.Variable, _variablesTable.Count - 1, $"variable <{_buffer}> of type <{variableType.Type}>");
											ClearBuffer();
										}
										else
										{
											AddLexem(LexemTypes.Variable, _variablesTable.FindIndex(c => c.Name == _buffer), $"variable <{_buffer}>");
											ClearBuffer();
										}
									}
									_state = LexemProcessorStates.Idle;
								}
								break;
							}
						case LexemProcessorStates.ReadingNum:
							{
								if (char.IsDigit(_charBuffer[0]))
								{
									AddToBuffer(_charBuffer[0]);
									GetNextChar();
								}
								else
								{
									AddLexem(LexemTypes.Constant, int.Parse(_buffer), $"integer with value = {_buffer}");
									ClearBuffer();
									_state = LexemProcessorStates.Idle;
								}
								break;
							}
						case LexemProcessorStates.Delimeter:
							{
								var searchResult = SearchInDelimeterDictionary();
								var searchOperatorsResult = SearchInOperationsDictionary();


								if (searchResult.Item1 != -1)
								{
									AddLexem(LexemTypes.Delimeter, searchResult.Item1, searchResult.Item2);
									_state = LexemProcessorStates.Idle;
									ClearBuffer();
								}
								else if (searchOperatorsResult.Item1 != -1) 
								{
									_seekingBuffer = new string(new char[] { _buffer[0], _charBuffer[0] });
									var seekOperatorsResult = SeekInOperationsDictionary();
									if (seekOperatorsResult.Item1 != -1)
									{
										AddLexem(LexemTypes.Operation, seekOperatorsResult.Item1, seekOperatorsResult.Item2);
										_state = LexemProcessorStates.Idle;
										ClearBuffer();
										GetNextChar();
									}
									else
									{
										AddLexem(LexemTypes.Operation, searchOperatorsResult.Item1, searchOperatorsResult.Item2);
										_state = LexemProcessorStates.Idle;
										ClearBuffer();
									}
								}
								else
								{
									AddLexem(LexemTypes.ParsingError, -1, $"Error at {_pointer}: Could not parse {_buffer}!");
									_state = LexemProcessorStates.Error;
								}
								break;
							}
						case LexemProcessorStates.Error:
							{
								_state = LexemProcessorStates.Final;
								break;
							}
						case LexemProcessorStates.Final:
							{
								return new Tuple<IList<Lexem>, IList<Variable>>(_lexems, _variablesTable);
							}
					}
				}

				return new Tuple<IList<Lexem>, IList<Variable>>(_lexems, _variablesTable);
			}
		}

		private void GetNextChar()
		{
			_reader.Read(_charBuffer, 0, 1);
			_pointer++;
		}

		private char PeekNextChar()
		{
			return (char) _reader.Peek();
		}

		private bool IsEmptyOrNextLine(char input)
		{
			return input == ' '
				|| input == '\n'
				|| input == '\t'
				|| input == '\0'
				|| input == '\r';
		}

		private void ClearBuffer()
		{
			_buffer = "";
			_seekingBuffer = "";
		}

		private void AddToBuffer(char input)
		{
			_buffer += input;
		}

		private void AddLexem(LexemTypes type, int value, string lex)
		{
			_lexems.Add(new Lexem(type, value, lex));
		}

		private (int, string) SearchInLexemDictionary()
		{
			var result = Array.FindIndex(Constants.Constants.Keywords, l => l.Equals(_buffer));

			if (result != -1)
			{
				return (result, _buffer);
			}

			return (-1, _buffer);
		}

		private (int, string) SearchInDelimeterDictionary()
		{
			var result = Array.FindIndex(Constants.Constants.KeySymbols, l => l.Equals(_buffer));

			if (result != -1)
			{
				return (result, _buffer);
			}

			return (-1, _buffer);
		}

		private (int, string) SearchInTypesDictionary() {
			var searchResult = Constants.Constants.Types.ContainsKey(_buffer);

			if (searchResult) {
				return Constants.Constants.Types[_buffer];
			}

			return (-1, _buffer);
		}

		private (int, string) SearchInOperationsDictionary() {
			var searchResult = Constants.Constants.Operators.ContainsKey(_buffer);

			if (searchResult) {
				return Constants.Constants.Operators[_buffer];
			}

			return (-1, _buffer);
		}

		private (int, string) SeekInOperationsDictionary()
		{
			var searchResult = Constants.Constants.Operators.ContainsKey(_seekingBuffer);

			if (searchResult)
			{
				return Constants.Constants.Operators[_seekingBuffer];
			}

			return (-1, _buffer);
		}
	}
}

