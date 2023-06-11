using McMaster.Extensions.CommandLineUtils;

namespace Adr.Commands;

public static class OpenCommand
{
    public static void Configure(CommandLineApplication app)
        => app.Command("open", newCmd =>
        {
            newCmd.Description = "Open your ADR folder in VS Code";
            var pathArgument = newCmd.Argument("path", "The absolute or relative path (optional, defaults to current directory)");

            newCmd.HelpOption();

            newCmd.OnExecute(() =>
            {
                var path = pathArgument.HasValue ? pathArgument.Value : Environment.CurrentDirectory;
                var tool = new AdrTool(path);
                tool.Run("open");
                return 0;
            });
        });
}
