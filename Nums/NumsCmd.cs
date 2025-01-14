using System.Numerics;
using RT.CommandLine;
using RT.Util;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    [Documentation("Modifies all numbers in the input and then outputs the result.")]
    public class NumsCmd : CmdToolsBase
    {
        [IsPositional, IsMandatory, Documentation("Specifies the arithmetic function in terms of x, for example “x + 2”.")]
        public string Expression = null;

        [Option("-r", "--round"), Documentation("Rounds the results to the specified number of decimal places.")]
        public int? Round = null;

        [Option("-i", "--integers-only"), Documentation("Calculations are performed on BigInteger instead of floating-point. By default, 2.5 is considered to be a decimal number; with this option, it will be seen as the number 2 and the number 5 separately.")]
        public bool IntsOnly;

        protected override int execute(TextReader input, TextWriter output)
        {
            try
            {
                if (IntsOnly)
                {
                    var node = ExpressionParser<BigInteger>.Parse(Expression, BigInteger.Parse, ["x"], [], ExpressionParser.OperatorsBi, ExpressionParser.FunctionsBi);
                    output.Write(input.ReadToEnd().RegexReplace(@"-?\d+", m => node.Evaluate(new Dictionary<string, BigInteger> { ["x"] = BigInteger.Parse(m.Value) }).ToString()));
                }
                else
                {
                    var node = ExpressionParser<double>.Parse(Expression, double.Parse, ["x"], ExpressionParser.Constants, ExpressionParser.OperatorsDbl, ExpressionParser.FunctionsDbl);
                    output.Write(input.ReadToEnd().RegexReplace(@"-?\d*\.?\d+", m => node.Evaluate(new Dictionary<string, double> { ["x"] = m.Value.ParseDouble() })
                        .Apply(result => Round == null ? result.ToString() : result.ToString($"0.{new string('#', Round.Value)}"))));
                }
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
