using System.Numerics;
using System.Text;
using RT.CommandLine;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    [Documentation("Performs a base conversion of an integer.")]
    public class BaseConverterCmd : CmdToolsBase, ICommandLineValidatable
    {
        [IsPositional, IsMandatory, Documentation("Specifies the integer to convert.")]
        public string Integer = null;

        [IsPositional, IsMandatory, Documentation("Specifies the base to convert from.")]
        public int BaseFrom = 10;

        [IsPositional, IsMandatory, Documentation("Specifies the base to convert to.")]
        public int BaseTo = 16;

        public ConsoleColoredString Validate()
        {
            if (BaseFrom < 2 || BaseFrom > 36)
                return new ConsoleColoredString($"Cannot convert from base {BaseFrom.ToString().Color(ConsoleColor.Magenta)}. Bases must be in the range {"2 – 36".Color(ConsoleColor.Green)}.");
            if (BaseTo < 2 || BaseTo > 36)
                return new ConsoleColoredString($"Cannot convert to base {BaseTo.ToString().Color(ConsoleColor.Magenta)}. Bases must be in the range {"2 – 36".Color(ConsoleColor.Green)}.");
            return null;
        }

        protected override int execute(TextReader input, TextWriter output)
        {
            var digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var valueStr = Integer;
            var negative = false;
            var value = BigInteger.Zero;
            if (valueStr.StartsWith('-'))
            {
                negative = true;
                valueStr = valueStr.Substring(1);
            }
            for (var i = 0; i < valueStr.Length; i++)
            {
                var d = digits.IndexOf(valueStr[i]);
                if (d == -1)
                {
                    Console.Error.WriteLine($"Invalid digit: {valueStr[i]}.");
                    return 2;
                }
                value = (value * BaseFrom) + (d % 36);
            }

            var result = new StringBuilder();
            while (value > 0)
            {
                result.Insert(0, digits[(int) (value % BaseTo)]);
                value /= BaseTo;
            }
            if (negative)
                result.Insert(0, "-");
            output.Write(result.ToString());
            return 0;
        }
    }
}
