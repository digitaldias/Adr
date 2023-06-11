using System.Reflection;
using Adr.Commands;
using Adr.VsCoding;
using McMaster.Extensions.CommandLineUtils;

namespace Adr;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandLineApplication
        {
            Name = "ADR",
            FullName = "ADR-Cli"
        };
        app.VersionOptionFromAssemblyAttributes(Assembly.GetExecutingAssembly());
        app.Description = "This is a command-line application for managing ADRs.";
        app.HelpOption("-h|--help");
        app.ExtendedHelpText = """

            You can use this application in two modes:
                1. Run the application with no parameters or with a path (absolute or relative) to start in default mode.
                2. Use the 'new' or 'open' subcommands in quickmode (you know what you're doing).
            """;

        var pathArgument = app.Argument("path", "The path to use. Defaults to current directory when not provided.");

        NewCommand.Configure(app);
        OpenCommand.Configure(app);

        app.OnExecute(() =>
        {
            if (!VSCode.IsVSCodeInstalled())
            {
                Cout.Fail("In order to use this tool, you must have {VsCode} installed and active in your {Path}", "Visual Studio Code", "PATH");
                return;
            }

            var path = string.IsNullOrEmpty(pathArgument.Value) ? Directory.GetCurrentDirectory() : pathArgument.Value;
            var tool = new AdrTool(path);
            if (!tool.Aborted)
            {
                tool.Run();
            }
        });

        return app.Execute(args);
    }
}
