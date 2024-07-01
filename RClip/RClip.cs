using System.Text;

internal class RClip
{
    [STAThread]
    private static void Main()
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        Console.Out.Write(Clipboard.GetText()); 
    }
}