using System.ComponentModel.DataAnnotations;
using Adr.Models;
using LibGit2Sharp;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableRow = Markdig.Extensions.Tables.TableRow;

namespace Adr;

public sealed class AdrTool
{
    private string _rootFolder;
    private string _docsFolder;

    public AdrTool(string givenPath)
    {
        if (!Path.Exists(givenPath))
        {
            Cout.Fail("The given path '{Path}' does not exist", givenPath);

            var message = $"The given path '{givenPath}' is not valid";
            throw new UnmatchedPathException(message);
        }

        if (string.IsNullOrEmpty(givenPath) || givenPath == ".")
        {
            givenPath = Environment.CurrentDirectory;
        }

        _rootFolder = GetGitRootFolder(givenPath);
        if (string.IsNullOrEmpty(_rootFolder))
        {
            Cout.Fail("The provided Path '{ProvidedPath}' is not a git repository", givenPath);

            var message = $"The given path '{givenPath}' is not a git repository";
            throw new ValidationException(message);
        }

        _docsFolder = GetDocsFolder(givenPath);
        if (string.IsNullOrEmpty(_docsFolder))
        {
            _docsFolder = AskAndCreateAdrFolder();
        }
    }

    private string AskAndCreateAdrFolder()
    {
        if (!AnsiConsole.Confirm("No ADR structure exists here. Create it?", defaultValue: false))
        {
            return string.Empty;
        }

        var adrFolder = Path.Combine(_rootFolder, "docs");
        if (!Directory.Exists(adrFolder))
        {
            Directory.CreateDirectory(adrFolder);
        }

        adrFolder = Path.Combine(adrFolder, "adr");
        if (!Directory.Exists(adrFolder))
        {
            Directory.CreateDirectory(adrFolder);
        }

        return adrFolder;
    }

    public void Run()
    {
        DisplayInfo();
        var filesInFolder = Directory.GetFiles(_docsFolder, "*.md");
        var indexFile = filesInFolder.FirstOrDefault(f => f.Contains("0000-index"));

        if (!string.IsNullOrEmpty(indexFile))
        {
            ProcessIndexFile(indexFile);
        }

    }

    private static void ProcessIndexFile(string indexFile)
    {
        var content = File.ReadAllText(indexFile);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(content, pipeline);
        var table = document.Descendants<MdTable>().FirstOrDefault();
        var adrEntries = new List<AdrEntry>();

        if (table is not null)
        {
            foreach (var row in table.Descendants<MdTableRow>().Skip(1))
            {
                var cells = row.Descendants<TableCell>().ToList();
                var number = string.Concat(cells[0].Descendants<LiteralInline>().Select(x => x.Content));
                var title = string.Concat(cells[1].Descendants<LiteralInline>().Select(x => x.Content));
                var link = cells[1].Descendants<LinkInline>().FirstOrDefault();

                adrEntries.Add(new()
                {
                    Number = int.Parse(number),
                    Title = title,
                    Url = link?.Url ?? string.Empty,
                });
            }
        }
    }

    private void DisplayInfo()
    {
        Cout.Cls();
        Cout.Title("ADR Tool");
        Cout.Hr("v1.0 by digitaldias. 2023");
        Cout.Success("Working in folder '{DocsFolder}'", _docsFolder);
        Cout.Hr();
    }

    private string GetDocsFolder(string? givenPath)
    {
        while (!string.IsNullOrEmpty(givenPath) && BothPathsShareRoot(givenPath, _rootFolder))
        {
            var sampler = Path.Combine(givenPath, "docs", "adr");
            if (Directory.Exists(sampler))
            {
                return sampler;
            }

            givenPath = Directory.GetParent(givenPath)?.FullName;
        }

        return string.Empty;
    }

    private static bool BothPathsShareRoot(string path1, string path2)
    {
        if (path1.StartsWith(path2, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (path2.StartsWith(path1, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string GetGitRootFolder(string givenPath)
    {
        try
        {
            using var repo = new Repository(Repository.Discover(givenPath));
            return repo.Info.WorkingDirectory;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
