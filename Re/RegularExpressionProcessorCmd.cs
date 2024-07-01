using System.Text.RegularExpressions;
using RT.CommandLine;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    [CommandLine, Documentation("Performs regular expression operations.")]
    public class RegularExpressionProcessorCmd : CmdToolsBase
    {
        [IsPositional, IsMandatory, Documentation("Specifies a regular expression to match against the input text.")]
        public string RegularExpression = null;

        [Option("-r", "--replace-with"), DocumentationEggsML("If specified, matches of &<<*RegularExpression*>>& are replaced with &<<*ReplaceWith*>>&, and lines that do not match &<<*RegularExpression*>>& are kept unaltered. *$$0* can be used to refer to the input string, *$$1* to the first capture group etc. If unspecified, lines that do not match &<<*RegularExpression*>>& are filtered out.")]
        public string ReplaceWith = null;

        [Option("-a", "--all"), Documentation("Specifies that the whole input should be matched as a single string, as opposed to each line.")]
        public bool All = false;

        [Option("-u", "--up-to"), DocumentationEggsML("Specifies a maximum number of replacements. Can only be used with ^*-r*^.")]
        public int? UpTo = null;

        [EnumOptions(EnumBehavior.MultipleValues)]
        public OptionFlags Options = 0;

        [Flags]
        public enum OptionFlags
        {
            //None = 0,
            [Option("-i", "--ignore-case"), Documentation("Case-insensitive match.")]
            IgnoreCase = 1,
            [Option("-m", "--multi-line"), DocumentationEggsML("Multiline mode: *^^* and *$$* match at the start and end of each line. (Only has an effect when combined with ^*-a*^.)")]
            Multiline = 2,
            [Option("-ec", "--explicit-capture"), DocumentationEggsML("Only explicitly named or numbered groups of the form *(?<<name>>...)* are considered capturing groups.")]
            ExplicitCapture = 4,
            [Option("-co", "--compiled"), Documentation("The regular expression is compiled instead of being interpreted.")]
            Compiled = 8,
            [Option("-s", "--single-line"), DocumentationEggsML("Single-line mode: *.* matches every character (instead of every character except *\n*).")]
            Singleline = 16,
            [Option("-x", "--ignore-pattern-whitespace"), DocumentationEggsML("Eliminates unescaped whitespace from the pattern and enables comments starting with *##*.")]
            IgnorePatternWhitespace = 32,
            [Option("-rl", "--right-to-left"), Documentation("Right-to-left matching mode.")]
            RightToLeft = 64,
            [Option("-es", "--ecma-script"), Documentation("ECMAScript-compliant behavior.")]
            ECMAScript = 256,
            [Option("-ci", "--culture-invariant"), Documentation("Cultural differences in language are ignored.")]
            CultureInvariant = 512,
            [Option("-n", "--non-backtracking"), Documentation("Uses an approach that avoids backtracking and guarantees linear-time processing.")]
            NonBacktracking = 1024
        }

        protected override int execute(TextReader input, TextWriter output)
        {
            var regex = new Regex(RegularExpression, (RegexOptions) Options);
            string replacer(string input) => ReplaceWith == null ? regex.IsMatch(input) ? input : null : UpTo == null ? regex.Replace(input, ReplaceWith) : regex.Replace(input, ReplaceWith, UpTo.Value);

            if (All)
                output.Write(replacer(input.ReadToEnd()));
            else
                foreach (var line in input.ReadLines())
                    if (replacer(line) is { } replaced)
                        output.WriteLine(replaced);

            return 0;
        }
    }
}
