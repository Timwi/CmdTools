namespace CmdTools
{
    public static class BaseConverter
    {
        [STAThread]
        private static int Main(string[] args) => RunCmdTool.Run<BaseConverterCmd>(args);
    }
}