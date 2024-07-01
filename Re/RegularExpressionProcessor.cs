namespace CmdTools
{
    public static class RegularExpressionProcessor
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // "$(TargetPath)" --post-build-check "$(SolutionDir)."
            if (args.Length == 2 && args[0] == "--post-build-check")
                return RT.PostBuild.PostBuildChecker.RunPostBuildChecks(args[1], System.Reflection.Assembly.GetExecutingAssembly());

            return RunCmdTool.Run<RegularExpressionProcessorCmd>(args);
        }
    }
}