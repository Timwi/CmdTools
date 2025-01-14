using System.Text;
using RT.CommandLine;
using RT.Util;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace CmdTools
{
    [Documentation("Outputs the contents of the clipboard.")]
    public class RClipCmd : CmdToolsBase, ICommandLineValidatable
    {
        [Option("-f", "--formats"), Documentation("Shows a list of the available data formats on the clipboard instead.")]
        public bool Formats = false;

        [IsPositional, Documentation("Specifies which format to read from the clipboard.")]
        public string Format = null;

        [Option("-h", "--hex-dump"), DocumentationEggsML("Outputs the clipboard contents in the form of a hex dump. Only valid if <*Format*> is specified and the data is binary.")]
        public bool HexDump = false;

        [Option("-t", "--type"), DocumentationEggsML("Outputs only the type of the clipboard contents.")]
        public bool Type = false;

        public enum EncodingEnum
        {
            [Option("-8", "--utf-8"), DocumentationEggsML("Assumes the clipboard contents are in UTF-8.")]
            UTF8,
            [Option("-le", "--utf-16-le"), DocumentationEggsML("Assumes the clipboard contents are in little-endian UTF-16.")]
            UTF16,
            [Option("-be", "--utf-16-be"), DocumentationEggsML("Assumes the clipboard contents are in big-endian UTF-16.")]
            UTF16BE
        }

        [EnumOptions(EnumBehavior.SingleValue)]
        public EncodingEnum UseEncoding = EncodingEnum.UTF8;

        public ConsoleColoredString Validate()
        {
            if (Formats && Format != null)
                return new ConsoleColoredString($"The {"-i".Color(ConsoleColor.White)} option and the {"Formats".Color(ConsoleColor.Green)} parameter cannot be used together.");
            if (Formats && HexDump)
                return new ConsoleColoredString($"The {"-f".Color(ConsoleColor.White)} and {"-h".Color(ConsoleColor.White)} options cannot be used together.");
            if (Type && Format == null)
                return new ConsoleColoredString($"The {"-t".Color(ConsoleColor.White)} option can only be used if the {"Formats".Color(ConsoleColor.Green)} option is also specified.");
            if (HexDump && Format == null)
                return new ConsoleColoredString($"The {"-h".Color(ConsoleColor.White)} option can only be used if the {"Formats".Color(ConsoleColor.Green)} option is also specified.");
            return null;
        }

        protected override int execute(TextReader input, TextWriter output)
        {
            if (Formats)
            {
                foreach (var format in Clipboard.GetDataObject().GetFormats())
                    output.WriteLine(format);
                return 0;
            }

            if (Format == null)
            {
                output.Write(Clipboard.GetText());
                return 0;
            }

            var clipboard = Clipboard.GetDataObject();
            if (!clipboard.GetDataPresent(Format))
            {
                ConsoleUtil.WriteLine(new ConsoleColoredString($"The format {Format.Color(ConsoleColor.Magenta)} is not on the clipboard."), stdErr: true);
                return 1;
            }

            var obj = clipboard.GetData(Format);
            if (Type)
            {
                output.WriteLine(obj.GetType().FullName);
                return 0;
            }

            if (obj is MemoryStream ms)
                obj = ms.ToArray();

            if (obj is byte[] bytes && HexDump)
            {
                foreach (var chunk in bytes.Split(64))
                    output.WriteLine(chunk.Select(b => b.ToString("X2")).JoinString(" "));
            }
            else if (obj is byte[] bytes2)
                output.Write((UseEncoding switch
                {
                    EncodingEnum.UTF8 => Encoding.UTF8,
                    EncodingEnum.UTF16 => Encoding.Unicode,
                    EncodingEnum.UTF16BE => Encoding.BigEndianUnicode,
                    _ => throw new NotImplementedException()
                }).GetString(bytes2));
            else if (obj is string str)
                output.Write(str);
            else
            {
                ConsoleUtil.WriteLine(new ConsoleColoredString($"The clipboard contents of format {Format.Color(ConsoleColor.Magenta)} are of type {obj.GetType().FullName.Color(ConsoleColor.Cyan)} which is not supported."), stdErr: true);
                return 2;
            }
            return 0;
        }
    }
}
