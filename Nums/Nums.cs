namespace CmdTools
{
    public static class Nums
    {
        [STAThread]
        private static int Main(string[] args) => RunCmdTool.Run<NumsCmd>(args);
    }
}
