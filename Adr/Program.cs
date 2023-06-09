using Adr.VsCoding;

namespace Adr;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Any(arg => arg.StartsWith("--help", StringComparison.InvariantCultureIgnoreCase)))
        {
            ShowHelp();
            return 0;
        }

        var path = args.Any() ? args[0] : Environment.CurrentDirectory;

        if (!VSCode.IsVSCodeInstalled())
        {
            Cout.Fail("In order to use this tool, you must have {VsCode} installed and active in your {Path}", "Visual Studio Code", "PATH");
            return 1;
        }

        var tool = new AdrTool(path);
        if (!tool.Aborted)
        {
            tool.Run();
        }

        return 0;
    }

    private static void ShowHelp()
    {
        Cout.Cls();
        Cout.Title("ADR-Tool");
        Cout.Hr();

        Cout.Info("Interactive tool for working with ADR records. Point to your solution folder to get started.");
        Cout.Info("Usage: {Adr} {Path}", "adr", "[[path]]");
    }
}
