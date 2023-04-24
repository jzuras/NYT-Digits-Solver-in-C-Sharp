using MathParserTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NYT_Digits
{
	#region Example Usage

	//		private static int[] testNumbers = { 4, 8, 5, 11, 15, 20 };
	//		private static int target = 133;
	//		DigitsSolver ds = new DigitsSolver();
	//		List<string> solution = ds.Solve(testNumbers, target);
	//		for (int i = 0; i < solution.Count; i++)
	//		{
	//			Console.WriteLine(solution[i]);
	//		}
	//		Console.WriteLine(solution.Count + " total soltions found.");

	#endregion

	public class DigitsSolver
	{
		/// <summary>
		/// constructor
		/// </summary>
		public DigitsSolver()
		{
		}

		/// <summary>
		/// Finds solutions to the New York Times game called Digits.
		/// The object of the game is to use the provided numbers and
		/// the mathematical operations of addition, subtraction,
		/// multiplication and division to produce and equation
		/// which evaluates to the given target number.
		/// In this version, all possible permutations of solutions
		/// are returned.
		/// </summary>
		/// <param name="aDigits">A list of integer numbers.</param>
		/// <param name="aTarget">The goal integer to reach.</param>
		/// <returns>List of all possible solutions which do not
		/// violate the 2 NYT rules: no operation can result in
		/// a negative or fraction value.</returns>
		public List<string> Solve(int[] aDigits, int aTarget)
		{
			List<string> solution = new List<string>();
			MathParser parser = new MathParser();

			// todo - any way to speed this up?

			// this will be a list of every possible combination of
			// integers 0 to length of aDigits (6 currently) minus 1
			// to include using fewer than all available digits
			List<List<int>> indexPermutations = 
				this.GetPermutations(aDigits.Length);

			// need to iterate over this list of possibilities,
			// and for each one, we need to try every possible permutation 
			// of operators

			// we need 1 fewer operators than we have digits
			// digit permutations include as few as 1 digit, up to Length
			// we need operator permutations for each length of digits possible
			List<ICollection<IList<char>>> operatorListList = 
				new List<ICollection<IList<char>>>();
			string operatorString = "+-*/";
			for (int i = 1; i < aDigits.Length; i++)
			{
				ICollection<IList<char>> output = 
					new Collection<IList<char>>();
				IList<char> item = new char[i];
				this.CombineWithRepetitions(output, operatorString, item, 0);
				operatorListList.Add(output);
			}

			// lists will give every permuation of both digits and operators,
			// so loop through each one to build each possible equation
			for (int digitPermIndex = 0; 
				digitPermIndex < indexPermutations.Count; digitPermIndex++)
			{
				// digit permutations can include any number of digits
				// if only 1 digit, treat as special case since there 
				// are no operators
				if (indexPermutations[digitPermIndex].Count <= 1)
				{
					// TODO - unlikely that a single digit is also the target
					// here at the top level
					continue;
				}

				ICollection<IList<char>> opsList = 
					operatorListList[indexPermutations[digitPermIndex].Count - 2];

				// concept: build all possible operations, then convert 
				// to RPN, then calculate answer. This allows evaluation
				// each step along the way to catch NYT rule violations.
				// NOTE - if a solution is not found, this likely means 
				// parens are needed in the answer, so code will need 
				// to be expanded to allow all possible paren combinations,
				// which RPN should handle.
				for (int operatorPermIndex = 0; 
					operatorPermIndex < opsList.Count; operatorPermIndex++)
				{
					List<char> operators = 
						opsList.ElementAt<IList<char>>(operatorPermIndex) 
						as List<char>;
					string equation = 
						aDigits[indexPermutations[digitPermIndex][0]]
						.ToString();
					bool ruleViolation = false;
					for (int digitIndex = 1; 
						digitIndex < indexPermutations[digitPermIndex].Count;
						digitIndex++)
					{
						equation += " " + operators[digitIndex - 1];
						equation += " " + 
							aDigits[indexPermutations[digitPermIndex][digitIndex]]
							.ToString();
					}

					// does this equation evaluate to the desired target?
					if (parser.Parse(equation, out ruleViolation) == aTarget)
					{
						if (ruleViolation == false)
						{
							solution.Add(equation);
						}
					}

				}
			}
			return solution;
		}

		#region Permutations

		// The two methods in this region are modified from the 
		// original string verion here:
		// https://stackoverflow.com/questions/33134788/permutations-with-repetition/33138356#33138356

		private List<List<int>> GetPermutations(int aLength)
		{
			List<List<int>> permutationsOfIndices = 
				this.GetNumericalPermutations(
				Enumerable.Range(0, aLength).ToList(), aLength);

			return permutationsOfIndices;
		}

		private List<List<int>> GetNumericalPermutations(List<int> aValues, 
			int aMaxLength)
		{
			if (aMaxLength == 1)
			{
				return aValues.Select(x => new List<int> { x }).ToList();
			}
			else
			{
				List<List<int>> permutations = 
					this.GetNumericalPermutations(aValues, aMaxLength - 1);

				foreach (int index in aValues)
				{
					List<List<int>> newPermutations = 
						permutations.Where(x => !x.Contains(index))
						.Select(x => x.Concat(new List<int> { index }))
						.Where(x => !permutations.Any(y => y.SequenceEqual(x)))
						.Select(x => x.ToList()).ToList();

					permutations.AddRange(newPermutations);
				}
				return permutations;
			}
		}

		#endregion

		#region Permutations with Repetitions

		// The method below was pulled from a GitHub repo 
		// https://gist.github.com/fdeitelhoff/5052611

		private void CombineWithRepetitions(ICollection<IList<char>> aOutput, 
			IEnumerable<char> aInput, IList<char> aItem, int aCount)
		{
			if (aCount < aItem.Count)
			{
				IList<char> enumerable = 
					aInput as IList<char> ?? aInput.ToList();
				foreach (char symbol in enumerable)
				{
					aItem[aCount] = symbol;
					this.CombineWithRepetitions(aOutput, enumerable, aItem, 
						aCount + 1);
				}
			}
			else
			{
				aOutput.Add(new List<char>(aItem));
			}
		}

		#endregion
	}
}
