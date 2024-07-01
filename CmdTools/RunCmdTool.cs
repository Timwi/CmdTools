using System.Text;
using RT.CommandLine;

namespace CmdTools
{
    public static class RunCmdTool
    {
        public static int Run<CmdLine>(string[] args) where CmdLine : CmdToolsBase
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch { }

            try
            {
                return CommandLineParser.Parse<CmdLine>(args).Execute();
            }
            catch (CommandLineParseException e)
            {
                e.WriteUsageInfoToConsole();
                return 1;
            }
        }
    }
}
