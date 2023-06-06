namespace Adr;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Any(arg => arg.StartsWith("--help")))
        {
            ShowHelp();
            return 0;
        }
        var path = args.Any() ? args[0] : Environment.CurrentDirectory;

        var tool = new AdrTool(path);

        return 0;
    }

    private static void ShowHelp()
    {
        Cout.Cls();
        Cout.Title("ADR-Tool");
        Cout.Hr();

        Console.WriteLine("Interactive tool for working with ADR records. Point to your solution folder to get started.");
        Console.WriteLine("Usage: adr [path]");
    }
}