using McMaster.Extensions.CommandLineUtils;

namespace Adr.Commands;
public static class NewCommand
{
    public static void Configure(CommandLineApplication app)
        => app.Command("new", newCmd =>
        {
            newCmd.Description = "Quickly add a new ADR";

            var titleArgument = newCmd.Argument("title", "The title of the new ADR", false);
            var pathArgument = newCmd.Argument("path", "Optional. Absolute or relative path to project. Current dir when omitted", false);

            newCmd.HelpOption();

            newCmd.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(titleArgument.Value))
                {
                    Cout.Warn("You must specify a title for the new command");
                    return 1;
                }

                var path = pathArgument.HasValue ? pathArgument.Value : Environment.CurrentDirectory;
                var tool = new AdrTool(path);
                tool.Run("new", titleArgument.Value);
                return 0;
            });
        });
}
