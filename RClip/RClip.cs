namespace CmdTools
{
    public static class RClip
    {
        [STAThread]
        private static int Main(string[] args) => RunCmdTool.Run<RClipCmd>(args);
    }
}