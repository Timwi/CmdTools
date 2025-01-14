using System.Numerics;
using RT.Generexes;
using RT.Util;
using S = RT.Generexes.Stringerex;

namespace CmdTools
{
    public static class ExpressionParser
    {
        public static readonly Dictionary<string, double> Constants = new()
        {
            ["pi"] = Math.PI,
            ["π"] = Math.PI,
            ["tau"] = Math.Tau,
            ["τ"] = Math.Tau,
            ["e"] = Math.E
        };

        // ** FUNCTIONS **

        public static readonly (int arity, string name, Func<double[], double> operation)[] FunctionsDbl = Ut.NewArray<(int arity, string name, Func<double[], double> operation)>(
            (1, "sin", ar => Math.Sin(ar[0])),
            (1, "cos", ar => Math.Cos(ar[0])),
            (1, "tan", ar => Math.Tan(ar[0])),
            (1, "sinh", ar => Math.Sinh(ar[0])),
            (1, "cosh", ar => Math.Cosh(ar[0])),
            (1, "tanh", ar => Math.Tanh(ar[0])),
            (1, "sqrt", ar => Math.Sqrt(ar[0])),
            (1, "floor", ar => Math.Floor(ar[0])),
            (1, "ceil", ar => Math.Ceiling(ar[0])),
            (1, "ln", ar => Math.Log(ar[0])),
            (1, "sqr", ar => ar[0] * ar[0]),
            (2, "pow", ar => Math.Pow(ar[0], ar[1])),
            (2, "log", ar => Math.Log(ar[1], ar[0])),
            (2, "atan2", ar => Math.Atan2(ar[0], ar[1])));

        public static readonly (int arity, string name, Func<BigInteger[], BigInteger> operation)[] FunctionsBi = Ut.NewArray<(int arity, string name, Func<BigInteger[], BigInteger> operation)>(
            (1, "sqr", ar => ar[0] * ar[0]),
            (2, "pow", ar => BigInteger.Pow(ar[0], (int) ar[1])),
            (3, "modpow", ar => BigInteger.ModPow(ar[0], ar[1], ar[2])));

        // ** OPERATORS **

        public static readonly OperatorGroup<double>[] OperatorsDbl = Ut.NewArray<OperatorGroup<double>>(
            // Lowest precedence: additive
            new BinaryOperatorGroup<double>(rightAssociative: false,
                    ("+", (a, b) => a + b),
                    ("-", (a, b) => a - b),
                    ("−", (a, b) => a - b)),

            // multiplicative
            new BinaryOperatorGroup<double>(rightAssociative: false,
                ("*", (a, b) => a * b),
                ("×", (a, b) => a * b),
                ("/", (a, b) => a / b),
                ("÷", (a, b) => a / b),
                ("%", (a, b) => a % b)),

            // unary
            new UnaryOperatorGroup<double>(
                ("+", d => d),
                ("-", d => -d),
                ("−", d => -d)),

            // exponentiation
            new BinaryOperatorGroup<double>(rightAssociative: true,
                ("^", Math.Pow),
                ("↑", Math.Pow)));

        public static readonly OperatorGroup<BigInteger>[] OperatorsBi = Ut.NewArray<OperatorGroup<BigInteger>>(
            // Lowest precedence: bitwise
            new BinaryOperatorGroup<BigInteger>(rightAssociative: false,
                ("|", (a, b) => a | b),
                ("&", (a, b) => a & b),
                ("^", (a, b) => a ^ b)),

            // additive
            new BinaryOperatorGroup<BigInteger>(rightAssociative: false,
                ("+", (a, b) => a + b),
                ("-", (a, b) => a - b),
                ("−", (a, b) => a - b)),

            // multiplicative
            new BinaryOperatorGroup<BigInteger>(rightAssociative: false,
                ("*", (a, b) => a * b),
                ("×", (a, b) => a * b),
                ("/", (a, b) => a / b),
                ("÷", (a, b) => a / b),
                ("%", (a, b) => a % b),
                ("<<", (a, b) => a << (int) b),
                (">>", (a, b) => a >> (int) b)),

            // unary
            new UnaryOperatorGroup<BigInteger>(
                ("+", d => d),
                ("-", d => -d),
                ("−", d => -d),
                ("~", d => ~d),
                ("!", d =>
                {
                    var r = BigInteger.One;
                    for (var i = BigInteger.One; i <= d; i++)
                        r *= i;
                    return r;
                })
        ),

            // exponentiation
            new BinaryOperatorGroup<BigInteger>(rightAssociative: true,
                ("**", (a, b) => BigInteger.Pow(a, (int) b)),
                ("↑", (a, b) => BigInteger.Pow(a, (int) b))));
    }

    // ** PARSER **

    public static class ExpressionParser<T>
    {
        public static ExpressionNode<T> Parse(string input, Func<string, T> convertFromString, string[] variables, Dictionary<string, T> constants, OperatorGroup<T>[] operators, (int arity, string name, Func<T[], T> operation)[] functions)
        {
            if (variables.FirstOrDefault(constants.ContainsKey) is string invalid)
                throw new InvalidOperationException($"The variable name “{invalid}” cannot be used because it is already a built-in constant.");

            var digit = (S) (ch => ch >= '0' && ch <= '9');
            var number = digit.RepeatGreedy().Then(((S) '.').OptionalGreedy()).Then(digit.RepeatGreedy(1)).Process(m => (ExpressionNode<T>) new ConstantNode<T>(convertFromString(m.Match)));
            var functionNameFirstCharacter = (S) (ch => ch == '_' || char.IsLetter(ch));
            var functionNameCharacter = (S) (ch => ch == '_' || char.IsLetterOrDigit(ch));
            var functionName = functionNameFirstCharacter.Then(functionNameCharacter.RepeatGreedy()).Process(m => m.Match);
            var whitespace = new S(c => char.IsWhiteSpace(c)).RepeatGreedy();

            Stringerex<ExpressionNode<T>> leftAssociativeBinaryOperators(Stringerex<ExpressionNode<T>> higherPrecedence, Stringerex<Func<T, T, T>> operators) =>
                higherPrecedence.ThenRaw(whitespace.Then(operators).ThenRaw(higherPrecedence, (op, node) => new { Op = op, ExpressionNode = node }).RepeatGreedy(),
                    (firstOperand, operands) => operands.Aggregate(firstOperand, (prev, next) => new BinaryOperatorNode<T> { Left = prev, Right = next.ExpressionNode, Operation = next.Op }));

            Stringerex<ExpressionNode<T>> rightAssociativeBinaryOperators(Stringerex<ExpressionNode<T>> higherPrecedence, Stringerex<Func<T, T, T>> operators) =>
                higherPrecedence.ThenRaw(whitespace.Then(operators), (node, op) => new { Op = op, ExpressionNode = node }).RepeatGreedy().ThenRaw(higherPrecedence,
                    (operands, lastOperand) => operands.Reverse().Aggregate(lastOperand, (prev, next) => new BinaryOperatorNode<T> { Left = next.ExpressionNode, Right = prev, Operation = next.Op }));

            var expression = Stringerex<ExpressionNode<T>>.Recursive(expr =>
            {
                S expect(char ch) => whitespace.ThenExpect(ch, m => throw new ExpressionParseException(m.Index, $"‘{ch}’ expected."));

                Stringerex<ExpressionNode<T>> build(int index)
                {
                    if (index == operators.Length)
                        return whitespace.Then(Generex.Ors(
                            number,
                            Generex.Ors(variables.Select(v => new S(v).Process(m => (ExpressionNode<T>) new VariableNode<T>(v)))),
                            Generex.Ors(constants.Select(kvp => ((S) kvp.Key).Process(m => (ExpressionNode<T>) new ConstantNode<T>(kvp.Value)))),
                            ((S) '(').Then(expr).Then(expect(')')),
                            Generex.Ors(functions.Select(fn => ((S) fn.name).Process(m => fn).Then(expect('('))
                                .ThenRaw(expr.RepeatWithSeparatorGreedy(whitespace.Then(',')), (fn, operands) => (fn, operands: operands.ToArray()))
                                .Then(expect(')'))
                                .Process(m => m.Result.fn.arity == m.Result.operands.Length ? (ExpressionNode<T>) new FunctionNode<T> { Operation = m.Result.fn.operation, Arguments = m.Result.operands } : throw new ExpressionParseException(m.Index, $"Function {m.Result.fn.name} expects {m.Result.fn.arity} arguments; {m.Result.operands.Length} given.")))),
                            S.End.Process<ExpressionNode<T>>(m => throw new ExpressionParseException(m.Index, "Unexpected end of expression: operand missing.")),
                            ((S) ')').Process<ExpressionNode<T>>(m => throw new ExpressionParseException(m.Index, "Expected a number, parenthesized expression, unary operator or function name.")),
                            new Stringerex<ExpressionNode<T>>(null).Where(m => throw new ExpressionParseException(m.Index, "Unrecognized unary operator or function name."))
                        )).Atomic();

                    return operators[index] switch
                    {
                        UnaryOperatorGroup<T> unaryOperators => whitespace.Then(Generex.Ors(unaryOperators.Info.Select(kvp => new S(kvp.syntax).Process(m => kvp.operation))))
                            .ThenRaw(build(index + 1), (func, node) => (ExpressionNode<T>) new UnaryOperatorNode<T> { Child = node, Operation = func }).Or(build(index + 1)),
                        BinaryOperatorGroup<T> binaryOperators => Generex.Ors(binaryOperators.Info.Select(kvp => new S(kvp.syntax).Process(m => kvp.operation)))
                            .Apply(inner => binaryOperators.RightAssociative ? rightAssociativeBinaryOperators(build(index + 1), inner) : leftAssociativeBinaryOperators(build(index + 1), inner)),
                        _ => throw new InvalidOperationException()
                    };
                }

                return build(0);
            }).Then(whitespace).Then(S.Ors(
                ((S) ')').LookAhead().Where(m => throw new ExpressionParseException(m.Index, "Extraneous closing parenthesis.")),
                S.End.LookAheadNegative().Where(m => throw new ExpressionParseException(m.Index, "Missing operator.")),
                new S()
            ));

            return expression.RawMatchExact(input);
        }
    }

    public abstract class OperatorGroup<T> { }
    public class UnaryOperatorGroup<T>(params (string syntax, Func<T, T> operation)[] info) : OperatorGroup<T>
    {
        public (string syntax, Func<T, T> operation)[] Info => info;
    }
    public class BinaryOperatorGroup<T>(bool rightAssociative, params (string syntax, Func<T, T, T> operation)[] info) : OperatorGroup<T>
    {
        public bool RightAssociative => rightAssociative;
        public (string syntax, Func<T, T, T> operation)[] Info => info;
    }

    public sealed class ExpressionParseException(int index, string message) : Exception(message)
    {
        public int Index { get; private set; } = index;
    }

    public abstract class ExpressionNode<T>
    {
        public abstract T Evaluate(Dictionary<string, T> variables);
    }
    public sealed class UnaryOperatorNode<T> : ExpressionNode<T>
    {
        public ExpressionNode<T> Child;
        public Func<T, T> Operation;
        public override T Evaluate(Dictionary<string, T> variables) => Operation(Child.Evaluate(variables));
    }
    public sealed class BinaryOperatorNode<T> : ExpressionNode<T>
    {
        public ExpressionNode<T> Left;
        public ExpressionNode<T> Right;
        public Func<T, T, T> Operation;
        public override T Evaluate(Dictionary<string, T> variables) => Operation(Left.Evaluate(variables), Right.Evaluate(variables));
    }
    public sealed class FunctionNode<T> : ExpressionNode<T>
    {
        public ExpressionNode<T>[] Arguments;
        public Func<T[], T> Operation;
        public override T Evaluate(Dictionary<string, T> variables) => Operation(Arguments.Select(arg => arg.Evaluate(variables)).ToArray());
    }
    public sealed class VariableNode<T>(string variableName) : ExpressionNode<T>
    {
        public override T Evaluate(Dictionary<string, T> variables) => variables[variableName];
    }
    public sealed class ConstantNode<T>(T value) : ExpressionNode<T>
    {
        public override T Evaluate(Dictionary<string, T> variables) => value;
    }
}
