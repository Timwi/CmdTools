using System.Text;

internal class WClip
{
    [STAThread]
    private static void Main()
    {
        try { Console.InputEncoding = Encoding.UTF8; } catch { }
        Clipboard.SetText(Console.In.ReadToEnd());
    }
}