using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


// NOTE !!!
// most of this code came from this CodeProject:
// https://www.codeproject.com/Tips/381509/Math-Parser-NET-Csharp
// I modified the computation section to handle NYT rules for the game:
// operations that produce fractions or negative numbers will not be accepted
// I also removed code that handled most of the things NYT doesn't use,
// like trig functions and exponents.


namespace MathParserTK
{
	#region License agreement

	// This is light, fast and simple to understand mathematical parser 
	// designed in one class, which receives as input a mathematical 
	// expression (System.String) and returns the output value (System.Double). 
	// For example, if you input string is "√(625)+25*(3/3)" then parser return double value — 50. 
	// Copyright (C) 2012-2013 Yerzhan Kalzhani, kirnbas@gmail.com

	// This program is free software: you can redistribute it and/or modify
	// it under the terms of the GNU General Public License as published by
	// the Free Software Foundation, either version 3 of the License, or
	// (at your option) any later version.

	// This program is distributed in the hope that it will be useful,
	// but WITHOUT ANY WARRANTY; without even the implied warranty of
	// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	// GNU General Public License for more details.

	// You should have received a copy of the GNU General Public License
	// along with this program.  If not, see <http://www.gnu.org/licenses/> 

	#endregion

	#region Example usage

	// public static void Main()
	// {     
	//     MathParser parser = new MathParser();
	//     string s1 = "pi+5*5+5*3-5*5-5*3+1E1";
	//     string s2 = "sin(cos(tg(sh(ch(th(100))))))";
	//     bool isRadians = false;
	//     double d1 = parser.Parse(s1, isRadians);
	//     double d2 = parser.Parse(s2, isRadians);
	//
	//     Console.WriteLine(d1); // 13.141592...
	//     Console.WriteLine(d2); // 0.0174524023974442
	//     Console.ReadKey(true); 
	// }   

	#endregion

	public class MathParser
	{
		#region Fields

		#region Markers (each marker should have length equals to 1)

		private const string NumberMaker = "#";
		private const string OperatorMarker = "$";

		#endregion

		#region Internal tokens

		private const string Plus = OperatorMarker + "+";
		private const string UnPlus = OperatorMarker + "un+";
		private const string Minus = OperatorMarker + "-";
		private const string UnMinus = OperatorMarker + "un-";
		private const string Multiply = OperatorMarker + "*";
		private const string Divide = OperatorMarker + "/";
		private const string LeftParent = OperatorMarker + "(";
		private const string RightParent = OperatorMarker + ")";

		#endregion

		#region Dictionaries (containts supported input tokens, exclude number)

		// Key -> supported input token, Value -> internal token or number

		/// <summary>
		/// Contains supported operators
		/// </summary>
		private readonly Dictionary<string, string> supportedOperators =
			new Dictionary<string, string>
            {
                { "+", Plus },                
                { "-", Minus },
                { "*", Multiply },
                { "/", Divide },
                { "(", LeftParent },
                { ")", RightParent }
            };

		#endregion

		private readonly char decimalSeparator;

		#endregion

		#region Constructors

		/// <summary>
		/// Initialize new instance of MathParser
		/// (symbol of decimal separator is read
		/// from regional settings in system)
		/// </summary>
		public MathParser()
		{
			try
			{
				decimalSeparator = 
					Char.Parse(System.Globalization.CultureInfo.CurrentCulture
					.NumberFormat.NumberDecimalSeparator);
			}
			catch (FormatException ex)
			{
				throw new FormatException(
					"Error: can't read char decimal separator from system, "
					+ "check your regional settings.", ex);
			}
		}

		/// <summary>
		/// Initialize new instance of MathParser
		/// </summary>
		/// <param name="decimalSeparator">Set decimal separator</param>
		public MathParser(char decimalSeparator)
		{
			this.decimalSeparator = decimalSeparator;
		}

		#endregion

		/// <summary>
		/// Produce result of the given math expression
		/// </summary>
		/// <param name="expression">Math expression 
		/// (infix/standard notation)</param>
		/// <param name="ruleViolation">Outputs TRUE if NYT rule is violated, 
		/// FALSE otherwise</param>
		/// <returns>Result, or zero if rule violation</returns>
		public double Parse(string expression, out bool ruleViolation)
		{
			ruleViolation = false;

			try
			{
				return Calculate(ConvertToRPN(FormatString(expression)));
			}
			catch (Exception)
			{
				ruleViolation = true;
				return 0.0;
			}
		}

		/// <summary>
		/// Produce formatted string by the given string
		/// </summary>
		/// <param name="expression">Unformatted math expression</param>
		/// <returns>Formatted math expression</returns>
		private string FormatString(string expression)
		{
			if (string.IsNullOrEmpty(expression))
			{
				throw new ArgumentNullException("Expression is null or empty");
			}

			StringBuilder formattedString = new StringBuilder();
			int balanceOfParenth = 0; // Check number of parenthesis

			// Format string in one iteration and check number of parenthesis
			// (this function do 2 tasks because performance priority)
			for (int i = 0; i < expression.Length; i++)
			{
				char ch = expression[i];

				if (ch == '(')
				{
					balanceOfParenth++;
				}
				else if (ch == ')')
				{
					balanceOfParenth--;
				}

				if (Char.IsWhiteSpace(ch))
				{
					continue;
				}
				else if (Char.IsUpper(ch))
				{
					formattedString.Append(Char.ToLower(ch));
				}
				else
				{
					formattedString.Append(ch);
				}
			}

			if (balanceOfParenth != 0)
			{
				throw new FormatException("Number of left and right parenthesis is not equal");
			}

			return formattedString.ToString();
		}

		#region Convert to Reverse-Polish Notation

		/// <summary>
		/// Produce math expression in reverse polish notation
		/// by the given string
		/// </summary>
		/// <param name="expression">Math expression in infix notation</param>
		/// <returns>Math expression in postfix notation (RPN)</returns>
		private string ConvertToRPN(string expression)
		{
			int pos = 0; // Current position of lexical analysis
			StringBuilder outputString = new StringBuilder();
			Stack<string> stack = new Stack<string>();

			// While there is unhandled char in expression
			while (pos < expression.Length)
			{
				string token = LexicalAnalysisInfixNotation(expression, ref pos);

				outputString = SyntaxAnalysisInfixNotation(token, outputString, stack);
			}

			// Pop all elements from stack to output string            
			while (stack.Count > 0)
			{
				// There should be only operators
				if (stack.Peek()[0] == OperatorMarker[0])
				{
					outputString.Append(stack.Pop());
				}
				else
				{
					throw new FormatException("Format exception,"
					+ " there is function without parenthesis");
				}
			}

			return outputString.ToString();
		}

		/// <summary>
		/// Produce token by the given math expression
		/// </summary>
		/// <param name="expression">Math expression in infix notation</param>
		/// <param name="pos">Current position in string for lexical analysis</param>
		/// <returns>Token</returns>
		private string LexicalAnalysisInfixNotation(string expression, ref int pos)
		{
			// Receive first char
			StringBuilder token = new StringBuilder();
			token.Append(expression[pos]);

			// If it is a operator
			if (supportedOperators.ContainsKey(token.ToString()))
			{
				// Determine it is unary or binary operator
				bool isUnary = pos == 0 || expression[pos - 1] == '(';
				pos++;

				switch (token.ToString())
				{
					case "+":
						return isUnary ? UnPlus : Plus;
					case "-":
						return isUnary ? UnMinus : Minus;
					default:
						return supportedOperators[token.ToString()];
				}
			}
			else if (Char.IsLetter(token[0]))
			{
				throw new ArgumentException("Unknown token");
			}
			else if (Char.IsDigit(token[0]) || token[0] == decimalSeparator)
			{
				// Read number

				// Read the whole part of number
				if (Char.IsDigit(token[0]))
				{
					while (++pos < expression.Length
					&& Char.IsDigit(expression[pos]))
					{
						token.Append(expression[pos]);
					}
				}
				else
				{
					// Because system decimal separator
					// will be added below
					token.Clear();
				}

				// Read the fractional part of number
				if (pos < expression.Length
					&& expression[pos] == decimalSeparator)
				{
					// Add current system specific decimal separator
					token.Append(CultureInfo.CurrentCulture
						.NumberFormat.NumberDecimalSeparator);

					while (++pos < expression.Length
					&& Char.IsDigit(expression[pos]))
					{
						token.Append(expression[pos]);
					}
				}

				return NumberMaker + token.ToString();
			}
			else
			{
				throw new ArgumentException("Unknown token in expression");
			}
		}

		/// <summary>
		/// Syntax analysis of infix notation
		/// </summary>
		/// <param name="token">Token</param>
		/// <param name="outputString">Output string (math expression in RPN)</param>
		/// <param name="stack">Stack which contains operators (or functions)</param>
		/// <returns>Output string (math expression in RPN)</returns>
		private StringBuilder SyntaxAnalysisInfixNotation(string token, StringBuilder outputString, Stack<string> stack)
		{
			// If it's a number just put to string            
			if (token[0] == NumberMaker[0])
			{
				outputString.Append(token);
			}
			else if (token == LeftParent)
			{
				// If its '(' push to stack
				stack.Push(token);
			}
			else if (token == RightParent)
			{
				// If its ')' pop elements from stack to output string
				// until find the ')'

				string elem;
				while ((elem = stack.Pop()) != LeftParent)
				{
					outputString.Append(elem);
				}
			}
			else
			{
				// While priority of elements at peek of stack >= (>) token's priority
				// put these elements to output string
				while (stack.Count > 0 &&
					Priority(token, stack.Peek()))
				{
					outputString.Append(stack.Pop());
				}

				stack.Push(token);
			}

			return outputString;
		}

		/// <summary>
		/// Is priority of token less (or equal) to priority of p
		/// </summary>
		private bool Priority(string token, string p)
		{
			return GetPriority(token) <= GetPriority(p);
		}

		/// <summary>
		/// Get priority of operator
		/// </summary>
		private int GetPriority(string token)
		{
			switch (token)
			{
				case LeftParent:
					return 0;
				case Plus:
				case Minus:
					return 2;
				case UnPlus:
				case UnMinus:
					return 6;
				case Multiply:
				case Divide:
					return 4;
				default:
					throw new ArgumentException("Unknown operator");
			}
		}

		#endregion

		#region Calculate expression in RPN

		/// <summary>
		/// Calculate expression in reverse-polish notation
		/// </summary>
		/// <param name="expression">Math expression in reverse-polish 
		/// notation</param>
		/// <returns>Result</returns>
		private double Calculate(string expression)
		{
			int pos = 0; // Current position of lexical analysis
			var stack = new Stack<double>(); // Contains operands

			// Analyse entire expression
			while (pos < expression.Length)
			{
				string token = LexicalAnalysisRPN(expression, ref pos);

				stack = SyntaxAnalysisRPN(stack, token);
			}

			// At end of analysis in stack should be only one operand (result)
			if (stack.Count > 1)
			{
				throw new ArgumentException("Excess operand");
			}

			return stack.Pop();
		}

		/// <summary>
		/// Produce token by the given math expression
		/// </summary>
		/// <param name="expression">Math expression in reverse-polish 
		/// notation</param>
		/// <param name="pos">Current position of lexical analysis</param>
		/// <returns>Token</returns>
		private string LexicalAnalysisRPN(string expression, ref int pos)
		{
			StringBuilder token = new StringBuilder();

			// Read token from marker to next marker

			token.Append(expression[pos++]);

			while (pos < expression.Length && expression[pos] != NumberMaker[0]
				&& expression[pos] != OperatorMarker[0])
			{
				token.Append(expression[pos++]);
			}

			return token.ToString();
		}

		/// <summary>
		/// Syntax analysis of reverse-polish notation
		/// </summary>
		/// <param name="stack">Stack which contains operands</param>
		/// <param name="token">Token</param>
		/// <returns>Stack which contains operands</returns>
		private Stack<double> SyntaxAnalysisRPN(Stack<double> stack, 
			string token)
		{
			// if it's operand then just push it to stack
			if (token[0] == NumberMaker[0])
			{
				stack.Push(double.Parse(token.Remove(0, 1)));
			}
			// Otherwise apply operator or function to elements in stack
			else if (NumberOfArguments(token) == 1)
			{
				double arg = stack.Pop();
				double rst;

				switch (token)
				{
					case UnPlus:
						rst = arg;
						break;
					case UnMinus:
						rst = -arg;
						break;
					default:
						throw new ArgumentException("Unknown operator");
				}

				stack.Push(rst);
			}
			else
			{
				// otherwise operator's number of arguments equals to 2

				double arg2 = stack.Pop();
				double arg1 = stack.Pop();

				double rst;

				// this is the place to enforce NTY rules.
				// throw exception if violation occurs Rules:
				// operations that produce fractions or negative numbers 
				// will not be accepted.

				switch (token)
				{
					case Plus:
						rst = arg1 + arg2;
						break;
					case Minus:
						if( arg2 > arg1)
						{
							throw new Exception(
								"NYT Rule Violation: result of operation "
							+ "cannot be negative.");
						}
						rst = arg1 - arg2;
						break;
					case Multiply:
						rst = arg1 * arg2;
						break;
					case Divide:
						if (arg2 == 0)
						{
							throw new DivideByZeroException(
								"Second argument is zero");
						}
						if( arg1 % arg2 != 0)
						{
							throw new Exception(
							"NYT Rule Violation: result of operation cannot "
							+ "be fractional.");
						}
						rst = arg1 / arg2;
						break;
					default:
						throw new ArgumentException("Unknown operator");
				}

				stack.Push(rst);
			}

			return stack;
		}

		/// <summary>
		/// Produce number of arguments for the given operator
		/// </summary>
		private int NumberOfArguments(string token)
		{
			switch (token)
			{
				case UnPlus:
				case UnMinus:
					return 1;
				case Plus:
				case Minus:
				case Multiply:
				case Divide:
					return 2;
				default:
					throw new ArgumentException("Unknown operator");
			}
		}

		#endregion
	}
}