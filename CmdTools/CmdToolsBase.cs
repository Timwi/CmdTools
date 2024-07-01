using System.Text;
using RT.CommandLine;

namespace CmdTools
{
    public abstract class CmdToolsBase
    {
        [Option("-c", "--clipboard"), Documentation("Use Clipboard for input and output instead of stdin/stdout.")]
        public bool UseClipboard = false;

        public int Execute()
        {
            var clipboardIn = UseClipboard ? Clipboard.GetText() : null;
            var clipboardOut = new StringBuilder();

            var ret = execute(
                UseClipboard ? new StringReader(clipboardIn) : Console.In,
                UseClipboard ? new StringWriter(clipboardOut) : Console.Out);

            if (UseClipboard)
                Clipboard.SetText(clipboardOut.ToString());
            return ret;
        }

        protected abstract int execute(TextReader input, TextWriter output);
    }
}
