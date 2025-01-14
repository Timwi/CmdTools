using System.Numerics;
using RT.CommandLine;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    public class CalculatorCmd : CmdToolsBase, ICommandLineValidatable
    {
        [IsPositional, Documentation("Specifies the expression to evaluate. If not specified, it is read from stdin.")]
        public string Expression = null;

        [Option("-r", "--round"), Documentation("Rounds the result to the specified number of decimal places.")]
        public int? Round = null;

        [Option("-i", "--integer", "--integers"), Documentation("Calculations are performed on BigInteger instead of floating-point.")]
        public bool Integers = false;

        [Option("-n", "--newline"), Documentation("Outputs a newline after the result.")]
        public bool Newline = false;

        public ConsoleColoredString Validate()
        {
            if (Integers && Round != null)
                return new ConsoleColoredString($"The {"-i".Color(ConsoleColor.White)} and {"-r".Color(ConsoleColor.White)} options cannot be used together.");
            return null;
        }

        protected override int execute(TextReader input, TextWriter output)
        {
            try
            {
                var inp = Expression ?? input.ReadToEnd();
                object result = Integers
                    ? ExpressionParser<BigInteger>.Parse(inp, BigInteger.Parse, [], [], ExpressionParser.OperatorsBi, ExpressionParser.FunctionsBi).Evaluate([])
                    : ExpressionParser<double>.Parse(inp, double.Parse, [], ExpressionParser.Constants, ExpressionParser.OperatorsDbl, ExpressionParser.FunctionsDbl).Evaluate([]);
                var outp = Integers || Round == null ? result.ToString() : ((double) result).ToString($"0.{new string('#', Round.Value)}");
                if (Newline)
                    output.WriteLine(outp);
                else
                    output.Write(outp);
                return 0;
            }
            catch (ExpressionParseException p)
            {
                ConsoleUtil.WriteLine(stdErr: true, value: Expression.Color(ConsoleColor.Yellow));
                ConsoleUtil.WriteLine(stdErr: true, value: new string(' ', p.Index) + "^".Color(ConsoleColor.Red));
                ConsoleUtil.WriteLine(stdErr: true, value: p.Message.Color(ConsoleColor.Magenta));
                return 2;
            }
        }
    }
}