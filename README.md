# NYT-Digits-Solver-in-C-Sharp
A C-Sharp version of a brute-force method to solve almost every NYT Digits puzzle so far.

This was a quick-hit pivot from my original idea to mimic how I solve the puzzles manually, since I wanted to get something posted here.

This version creates equations using every permutation of digits in the puzzle, combined with every permutation with repetition of operatoes allowed. It then uses RPN to solve each equation, tossing out ones that violate the rules given by the New York Times for their game.

Unfortunately, there is one puzzle so far that it cannot solve, because the solution needs parens to add digits and using the sum in multiplication. A future version may address this.

As this is C-Sharp code, there is no Pages to run it. This too will be handled soon, perhaps in a different repo.
