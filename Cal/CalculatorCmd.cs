using RT.CommandLine;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    public class CalculatorCmd : CmdToolsBase
    {
        [IsPositional, Documentation("Specifies the expression to evaluate. If not specified, it is read from stdin.")]
        public string Expression = null;

        [Option("-r", "--round"), Documentation("Rounds the result to the specified number of decimal places.")]
        public int? Round = null;

        protected override int execute(TextReader input, TextWriter output)
        {
            try
            {
                var result = ExpressionParser.Parse(Expression ?? input.ReadToEnd()).Evaluate([]);
                output.Write(Round == null ? result.ToString() : result.ToString($"0.{new string('#', Round.Value)}"));
                return 0;
            }
            catch (ExpressionParser.ParseException p)
            {
                ConsoleUtil.WriteLine(stdErr: true, value: Expression.Color(ConsoleColor.Yellow));
                ConsoleUtil.WriteLine(stdErr: true, value: new string(' ', p.Index) + "^".Color(ConsoleColor.Red));
                ConsoleUtil.WriteLine(stdErr: true, value: p.Message.Color(ConsoleColor.Magenta));
                return 2;
            }
        }
    }
}