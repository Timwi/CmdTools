using RT.Generexes;
using RT.Util;
using S = RT.Generexes.Stringerex;
using SN = RT.Generexes.Stringerex<CmdTools.ExpressionParser.ExpressionNode>;

namespace CmdTools
{
    public static class ExpressionParser
    {
        public static ExpressionNode Parse(string input, params string[] variables)
        {
            var unaryFunctionsRaw = new Dictionary<string, Func<double, double>>
            {
                { "sin", Math.Sin },
                { "cos", Math.Cos },
                { "tan", Math.Tan },
                { "sinh", Math.Sinh},
                { "cosh", Math.Cosh },
                { "tanh", Math.Tanh },
                { "sqrt", Math.Sqrt },
                { "sqr", x => x * x }
            };
            var unaryFunctions = unaryFunctionsRaw.Select(kvp => new S(kvp.Key).Process(m => kvp.Value));

            var unaryOperatorsRaw = new Dictionary<string, Func<double, double>>
            {
                { "+", d => d },
                { "-", d => -d }
            };
            var unaryOperators = unaryOperatorsRaw.Select(kvp => new S(kvp.Key).Process(m => kvp.Value));

            var constantsRaw = new Dictionary<string, double>
            {
                { "pi", Math.PI },
                { "tau", Math.Tau },
                { "e", Math.E }
            };

            if (variables.FirstOrDefault(constantsRaw.ContainsKey) is string inv)
                throw new InvalidOperationException($"The variable name “{inv}” cannot be used because it is already a built-in constant.");

            var digit = (S) (ch => ch >= '0' && ch <= '9');
            var number = digit.RepeatGreedy().Then(new S('.').Then(digit.RepeatGreedy(1)).OptionalGreedy()).Where(m => m.Length > 0).Process(m => (ExpressionNode) new NumberNode { Number = Convert.ToDouble(m.Match) });
            var functionNameFirstCharacter = (S) (ch => ch == '_' || char.IsLetter(ch));
            var functionNameCharacter = (S) (ch => ch == '_' || char.IsLetterOrDigit(ch));
            var functionName = functionNameFirstCharacter.Then(functionNameCharacter.RepeatGreedy()).Process(m => m.Match);

            var expression = SN.Recursive(expr =>
            {
                var leftAssociativeBinaryOperators = Ut.Lambda((SN higherPrecedence, Stringerex<Func<double, double, double>> operators) =>
                    higherPrecedence.ThenRaw(operators.ThenRaw(higherPrecedence, (op, node) => new { Op = op, ExpressionNode = node }).RepeatGreedy(),
                        (firstOperand, operands) => operands.Aggregate(firstOperand, (prev, next) => new BinaryOperatorNode { Left = prev, Right = next.ExpressionNode, Operation = next.Op })));

                var rightAssociativeBinaryOperators = Ut.Lambda((SN higherPrecedence, Stringerex<Func<double, double, double>> operators) =>
                    higherPrecedence.ThenRaw(operators, (node, op) => new { Op = op, ExpressionNode = node }).RepeatGreedy().ThenRaw(higherPrecedence,
                        (operands, lastOperand) => operands.Reverse().Aggregate(lastOperand, (prev, next) => new BinaryOperatorNode { Left = next.ExpressionNode, Right = prev, Operation = next.Op })));

                var expectCloseParen = ((S) ')')
                    .Or(S.End.Where(m => throw new ParseException(m.Index, "Unexpected end of expression: ')' missing.")))
                    .Or(new S().Where(m => throw new ParseException(m.Index, "Unrecognized operator.")));

                var expectOpenParen = ((S) '(')
                    .Or(S.End.Where(m => throw new ParseException(m.Index, "Unexpected end of expression: '(' missing.")))
                    .Or(new S().Where(m => throw new ParseException(m.Index, "'(' expected.")));

                var power = SN.Recursive(pwr =>
                {
                    var primaryExpr = Generex.Ors(
                        number,
                        SN.Ors(variables.Select(v => new S(v).Process(m => (ExpressionNode) new VariableNode { VariableName = v }))),
                        Generex.Ors(constantsRaw.Select(kvp => ((S) kvp.Key).Process(m => (ExpressionNode) new ConstantNode(kvp.Value)))),
                        ((S) '(').Then(expr).Then(expectCloseParen),
                        Generex.Ors(unaryFunctions).Then(expectOpenParen).ThenRaw(expr, (func, node) => (ExpressionNode) new UnaryOperatorNode { Child = node, Operation = func }).Then(expectCloseParen),
                        Generex.Ors(unaryOperators).ThenRaw(pwr, (func, node) => (ExpressionNode) new UnaryOperatorNode { Child = node, Operation = func }),
                        S.End.Process<ExpressionNode>(m => throw new ParseException(m.Index, "Unexpected end of expression: operand missing.")),
                        ((S) ')').Process<ExpressionNode>(m => throw new ParseException(m.Index, "Expected a number, parenthesised expression, unary operator or function name.")),
                        new SN(null).Where(m => throw new ParseException(m.Index, "Unrecognized unary operator or function name."))
                    ).Atomic();

                    return rightAssociativeBinaryOperators(primaryExpr, ((S) '^').Process(m => new Func<double, double, double>(Math.Pow)));
                });

                var multiplicative = leftAssociativeBinaryOperators(power, Generex.Ors(
                    ((S) '*').Process(m => Ut.Lambda((double a, double b) => a * b)),
                    ((S) '/').Process(m => Ut.Lambda((double a, double b) => a / b))));

                var additive = leftAssociativeBinaryOperators(multiplicative, Generex.Ors(
                    ((S) '+').Process(m => Ut.Lambda((double a, double b) => a + b)),
                    ((S) '-').Process(m => Ut.Lambda((double a, double b) => a - b))
                ));

                return additive;
            }).Then(S.Ors(
                ((S) ')').LookAhead().Where(m => throw new ParseException(m.Index, "Extraneous closing parenthesis.")),
                S.End.LookAheadNegative().Where(m => throw new ParseException(m.Index, "Missing operator.")),
                new S()
            ));

            return expression.RawMatchExact(input);
        }

        public sealed class ParseException(int index, string message) : Exception(message)
        {
            public int Index { get; private set; } = index;
        }

        public abstract class ExpressionNode
        {
            public abstract double Evaluate(Dictionary<string, double> variables);
        }
        public sealed class UnaryOperatorNode : ExpressionNode
        {
            public ExpressionNode Child;
            public Func<double, double> Operation;
            public override double Evaluate(Dictionary<string, double> variables) => Operation(Child.Evaluate(variables));
        }
        public sealed class BinaryOperatorNode : ExpressionNode
        {
            public ExpressionNode Left;
            public ExpressionNode Right;
            public Func<double, double, double> Operation;
            public override double Evaluate(Dictionary<string, double> variables) => Operation(Left.Evaluate(variables), Right.Evaluate(variables));
        }
        public sealed class NumberNode : ExpressionNode
        {
            public double Number;
            public override double Evaluate(Dictionary<string, double> variables) { return Number; }
        }
        public sealed class VariableNode : ExpressionNode
        {
            public string VariableName;
            public override double Evaluate(Dictionary<string, double> variables) => variables[VariableName];
        }
        public sealed class ConstantNode(double value) : ExpressionNode
        {
            public override double Evaluate(Dictionary<string, double> variables) => value;
        }
    }
}
