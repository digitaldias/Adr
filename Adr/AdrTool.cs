using System.Globalization;
using Adr.Markdowners;
using Adr.Models;
using Adr.VsCoding;
using Adr.Writing;
using LibGit2Sharp;
using Spectre.Console;

namespace Adr;

public sealed class AdrTool
{
    private readonly string _rootFolder = string.Empty;
    private readonly string _docsFolder = string.Empty;
    private string? _indexFile;

    public AdrTool(string givenPath)
    {
        if (!Path.Exists(givenPath))
        {
            Cout.Fail("Error: The given path '{Path}' does not exist", givenPath);
            return;
        }

        if (string.IsNullOrEmpty(givenPath) || givenPath == ".")
        {
            givenPath = Environment.CurrentDirectory;
        }

        _rootFolder = GetGitRootFolder(givenPath);
        if (string.IsNullOrEmpty(_rootFolder))
        {
            Cout.Fail("Error: The provided Path '{ProvidedPath}' is not within a git repository", givenPath);
            Aborted = true;
            return;
        }

        _docsFolder = GetDocsFolder(givenPath);
        if (string.IsNullOrEmpty(_docsFolder))
        {
            _docsFolder = AskForAndCreateAdrFolder();
        }
    }

    public bool Aborted { get; private set; }

    public void Run()
    {
        DisplayInfo();
        var filesInFolder = Directory.GetFiles(_docsFolder, "*.md");
        _indexFile = Array.Find(filesInFolder, f => f.Contains("0000-index"));

        if (string.IsNullOrEmpty(_indexFile))
        {
            Cout.Fail("Ubable to locate ADR index in '{Path}'", _docsFolder);
            return;
        }

        var entries = IndexManipulator.GetEntriesFromIndexFile(_indexFile);

        if (entries.Any())
        {
            new LiveDataTable<AdrEntry>()
                .WithHeader($"There are {entries.Count} Architecture decision(s) made so far in this solution\nADR path: [yellow]{_docsFolder}[/]\n")
                .WithDataSource(entries)
                .WithColumns("Id", "Title")
                .WithDataPicker(e => new List<string> { e.Number.ToString(CultureInfo.InvariantCulture), e.Title })
                .WithEnterInstruction("open ADR #{0} in VS Code.\nUse arrow up/down to select.\n", p => p.Number.ToString(CultureInfo.InvariantCulture))
                .WithMultipleActions(new[]
                {
                    new LiveKeyAction<AdrEntry>('a', "Add new", _ => AppendNew(entries)),
                    new LiveKeyAction<AdrEntry>('r', "Rename selected", entry => Rename(entry, entries)),
                    new LiveKeyAction<AdrEntry>('i', "Recreate index from folder", _ => IndexManipulator.RecreateIndex(_docsFolder)),
                    new LiveKeyAction<AdrEntry>('o', "Open ADR folder in VS Code", _ => VSCode.OpenFolder(_docsFolder))
                })
                .WithSelectionAction(entry => VSCode.OpenFile(Path.Combine(_docsFolder, entry.Url)))

                .Start();
        }
    }

    private string AskForAndCreateAdrFolder()
    {
        if (!AnsiConsole.Confirm("Did not find ADR document folder structure. Should I create it?", defaultValue: false))
        {
            Cout.Info("Ok, aborting.");
            Aborted = true;
            return string.Empty;
        }

        var adrFolder = Path.Combine(_rootFolder, "Docs");
        if (!Directory.Exists(adrFolder))
        {
            Directory.CreateDirectory(adrFolder);
            Cout.Success("Created folder: {Folder}", adrFolder);
        }

        adrFolder = Path.Combine(adrFolder, "Adr");
        if (!Directory.Exists(adrFolder))
        {
            Directory.CreateDirectory(adrFolder);
            Cout.Success("Created folder: {Folder}", adrFolder);

            IndexManipulator.CreateInitialFolderContent(adrFolder);
        }

        return adrFolder;
    }

    private void Rename(AdrEntry entry, List<AdrEntry> entries)
    {
        var originalTitle = entry.Title;
        var originalUrl = entry.Url;
        var originalFilePath = Path.Combine(_docsFolder, originalUrl);

        var newTitle = AnsiConsole.Ask<string>("New title: ");
        entry.Title = newTitle;
        entry.Url = $"./{entry.Number:0000}-{newTitle.Replace(" ", "-").ToLower(CultureInfo.InvariantCulture)}.md";
        var destinationPath = Path.Combine(_docsFolder, entry.Url);

        var content = File.ReadAllText(originalFilePath);
        Cout.Info("Opened {file}", originalFilePath);

        content = content.Replace(originalTitle, newTitle);
        Cout.Info("Title updated to '{title}'", newTitle);

        File.WriteAllText(destinationPath, content);
        Cout.Info("Saved as '{file}'", destinationPath);

        File.Delete(originalFilePath);
        Cout.Info("Deleted '{file}'", originalFilePath);

        IndexManipulator.Rewrite(entries, _indexFile!);
    }

    private void AppendNew(List<AdrEntry> allEntries)
    {
        var nextNumber = allEntries.Max(e => e.Number) + 1;
        var newName = AnsiConsole.Ask<string>("Enter a [yellow]title[/] for the new ADR: ");
        var newFileName = $"{nextNumber:0000}-{newName.Replace(" ", "-").ToLower(CultureInfo.InvariantCulture)}.md";
        var newEntry = new AdrEntry
        {
            Number = nextNumber,
            Title = newName,
            Url = newFileName
        };

        Cout.Success("New entry #{Number} titled '{Title}' will be created as {Path}", newEntry.Number, newEntry.Title, newEntry.Url);

        if (AnsiConsole.Confirm("Do you want to Proceed?", defaultValue: false))
        {
            var created = DocumentCreator.Create(_docsFolder, newEntry);
            if (string.IsNullOrEmpty(created))
            {
                Cout.Fail("Somehow, ths failed. I'm at a loss here!");
                return;
            }

            allEntries.Add(newEntry);
            IndexManipulator.Rewrite(allEntries, _indexFile!);
            VSCode.OpenFile(created);
        }
    }

    private static void DisplayInfo()
    {
        Cout.Hr("ADR Tool v1.0.0 - 2023 - digitaldias");
        Cout.Info(" ");
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
        => path1.StartsWith(path2, StringComparison.InvariantCultureIgnoreCase)
        || path2.StartsWith(path1, StringComparison.InvariantCultureIgnoreCase);

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
