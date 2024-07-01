# Command Line Tools

Contains my own collection of command-line tools that I personally like.

* **Base:** Converts numbers between numerical bases (e.g. decimal to hexadecimal).
* **Cal:** Calculates an expression.
* **Nums:** Replaces all occurrences of numbers in an input with the result of a calculation (e.g. double all numbers).
* **Re:** Performs regular expression search or replace on an input.

## Clipboard integration

All tools listed above take a `-c` option to take input from the clipboard and write output to the clipboard instead of using stdin/stdout. Alternatively, if only one of these options is desired, use one of these:

* **Rclip:** Reads from the clipboard and outputs to stdout.
* **Wclip:** Reads from stdin and writes to the clipboard.

Example: `echo [1, 2, 3] | nums 2*x | wclip` will put the text `[2, 4, 6]` in the clipboard.