namespace CmdTools
{
    public static class Calculator
    {
        [STAThread]
        private static int Main(string[] args) => RunCmdTool.Run<CalculatorCmd>(args);
    }
}