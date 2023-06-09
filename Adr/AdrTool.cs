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
    private readonly SortedList<int, AdrEntry> _adrEntries = new();
    private readonly string _gitRootPath = string.Empty;
    private readonly string _adrFolder = string.Empty;
    private readonly string? _indexFile;

    public AdrTool(string givenPath)
    {
        if (string.IsNullOrEmpty(givenPath) || givenPath == ".")
        {
            givenPath = Environment.CurrentDirectory;
        }

        if (!Path.Exists(givenPath))
        {
            Cout.Fail("Error: The given path '{Path}' does not exist", givenPath);
            return;
        }

        _gitRootPath = GetGitRootFolder(givenPath);
        if (string.IsNullOrEmpty(_gitRootPath))
        {
            Cout.Fail("Error: The provided Path '{ProvidedPath}' is not within a git repository", givenPath);
            Aborted = true;
            return;
        }

        _adrFolder = GetAdrFolder(givenPath);

        if (string.IsNullOrEmpty(_adrFolder))
        {
            _adrFolder = AskForAndCreateAdrFolder();

            var newEntry = new AdrEntry
            {
                Number = 1,
                Title = "Record Architecture Decision Records",
                FilePath = "0001-record-architecture-decision-records.md",
                SupersededBy = 0
            };

            DocumentCreator.Create(_adrFolder, newEntry, DocumentCreator.DecisionToUseAdrAsMarkdown);

            _adrEntries.Add(1, newEntry);
            _indexFile = IndexManipulator.CreateIndexFromAdrEntries(_adrEntries, _adrFolder);
        }
        else
        {
            _indexFile = Path.Combine(_adrFolder, "0000-index.md");
        }
    }

    public bool Aborted { get; private set; }

    public void Run()
    {
        DisplayInfo();

        var entries = IndexManipulator.ReadAdrEntries(_adrFolder);
        foreach (var entry in entries)
        {
            if (!_adrEntries.ContainsKey(entry.Number))
            {
                _adrEntries.Add(entry.Number, entry);
            }
        }

        var tableMenu = new[]
        {
            new LiveKeyAction<AdrEntry>('a', "Add new", _ => AppendNew()),
            new LiveKeyAction<AdrEntry>('r', "Rename selected", entry => Rename(entry)),
            new LiveKeyAction<AdrEntry>('i', "Recreate index from folder", _ => IndexManipulator.RecreateIndex(_adrFolder)),
            new LiveKeyAction<AdrEntry>('o', "Open ADR folder in VS Code", _ => VSCode.OpenFolder(_adrFolder)),
            new LiveKeyAction<AdrEntry>('s', "Supersede with new ADR entry", e => Supersede(e))
        };

        if (_adrEntries.Any())
        {
            new LiveDataTable<AdrEntry>()
                .WithHeader($"There are {entries.Count()} Architecture decision(s) made so far in this solution\nADR path: [yellow]{_adrFolder}[/]\n")
                .WithDataSource(entries)
                .WithColumns("Id", "Title")
                .WithDataPicker(e => new List<string> { e.Number.ToString(CultureInfo.InvariantCulture), e.Title })
                .WithEnterInstruction("open ADR #{0} in VS Code.\nUse arrow up/down to select.\n", p => p.Number.ToString(CultureInfo.InvariantCulture))
                .WithMultipleActions(tableMenu)
                .WithSelectionAction(entry => VSCode.OpenFile(Path.Combine(_adrFolder, entry.FilePath)))
                .Start();
        }
    }

    private void Supersede(AdrEntry existingEntry)
    {
        Cout.Info("Supersede Entry titled: {existing}", existingEntry.Title);
        var confirmd = AnsiConsole.Confirm($"Create a new Entry that supersedes #{existingEntry.Number}?", defaultValue: false);
        if (!confirmd)
        {
            Cout.Warn("Supersede '{title}' aborted", existingEntry.Title);
            return;
        }

        var title = AnsiConsole.Ask<string>("[yellow]New title:[/] ", existingEntry.Title);
        var newEntry = CreateNewAdrFromTitle(title);
        existingEntry.SupersededBy = newEntry.Number;

        _adrEntries.Add(newEntry.Number, newEntry);

        var created = DocumentCreator.Create(_adrFolder, newEntry);

        IndexManipulator.CreateIndexFromAdrEntries(_adrEntries, _adrFolder);

        VSCode.OpenFile(created);
    }

    private AdrEntry CreateNewAdrFromTitle(string title)
    {
        var nextNumber = _adrEntries.Max(e => e.Key) + 1;
        var fileNamePart = title.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLower(CultureInfo.InvariantCulture).Trim();

        return new AdrEntry
        {
            Number = nextNumber,
            Title = title,
            FilePath = $"./{nextNumber:0000}-{fileNamePart}.md",
            SupersededBy = 0
        };
    }

    private string AskForAndCreateAdrFolder()
    {
        if (!AnsiConsole.Confirm("Did not find ADR document folder structure.\nDo you want to create it now?", defaultValue: false))
        {
            Cout.Info("Ok, aborting.");
            Aborted = true;
            return string.Empty;
        }

        var adrFolder = Path.Combine(_gitRootPath, "Docs");
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
        }

        return adrFolder;
    }

    private void Rename(AdrEntry entry)
    {
        var originalTitle = entry.Title;
        var originalUrl = entry.FilePath;
        var originalFilePath = Path.Combine(_adrFolder, originalUrl);

        Cout.Info("Rename entry: {EntryTitle}\n", entry.Title);
        var newTitle = AnsiConsole.Ask<string>("[yellow]New title:[/] ", entry.Title);
        entry.Title = newTitle;
        entry.FilePath = $"./{entry.Number:0000}-{newTitle.Replace(" ", "-").ToLower(CultureInfo.InvariantCulture)}.md";
        var destinationPath = Path.Combine(_adrFolder, entry.FilePath);

        var content = File.ReadAllText(originalFilePath);
        Cout.Info("Opened {file}", originalFilePath);

        content = content.Replace(originalTitle, newTitle);
        Cout.Info("Title updated to '{title}'", newTitle);

        File.WriteAllText(destinationPath, content);
        Cout.Info("Saved as '{file}'", destinationPath);

        if (!destinationPath.Equals(originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(originalFilePath);
            Cout.Info("Deleted '{file}'", originalFilePath);
        }

        IndexManipulator.Rewrite(_adrEntries, _indexFile!);
    }

    private void AppendNew()
    {
        var nextNumber = _adrEntries.Max(e => e.Value.Number) + 1;
        Cout.Info("Create New");
        var newName = AnsiConsole.Ask<string>("Enter a descriptive [yellow]Title[/] for your ADR: ");
        var newFileName = $"./{nextNumber:0000}-{newName.Replace(" ", "-").ToLower(CultureInfo.InvariantCulture)}.md";
        var newEntry = new AdrEntry
        {
            Number = nextNumber,
            Title = newName,
            FilePath = newFileName,
            SupersededBy = 0
        };

        Cout.Success("Entry #{Number}: '{Title}' will be created as {Path}", newEntry.Number, newEntry.Title, newEntry.FilePath);

        if (AnsiConsole.Confirm("Do you want to Proceed?", defaultValue: false))
        {
            var created = DocumentCreator.Create(_adrFolder, newEntry);
            if (string.IsNullOrEmpty(created))
            {
                Cout.Fail("Somehow, ths failed. I'm at a loss here!");
                return;
            }

            _adrEntries.Add(newEntry.Number, newEntry);
            IndexManipulator.Rewrite(_adrEntries, _indexFile!);
            VSCode.OpenFile(created);
        }
        else
        {
            Cout.Warn("No document created. Program finished");
        }
    }

    private static void DisplayInfo()
    {
        Cout.Cls();
        Cout.Hr("ADR Tool v1.0.0 - 2023 - digitaldias");
        Cout.Info(" ");
    }

    private string GetAdrFolder(string? givenPath)
    {
        while (!string.IsNullOrEmpty(givenPath) && BothPathsShareRoot(givenPath, _gitRootPath))
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
