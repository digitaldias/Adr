using System.ComponentModel.DataAnnotations;
using Adr.Markdowners;
using Adr.Models;
using Adr.VsCoding;
using Adr.Writing;
using LibGit2Sharp;
using Spectre.Console;

namespace Adr;

public sealed class AdrTool
{
    private string _rootFolder;
    private string _docsFolder;
    private string? _indexFile;

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
            _docsFolder = AskForAndCreateAdrFolder();
        }
    }

    private string AskForAndCreateAdrFolder()
    {
        if (!AnsiConsole.Confirm("No ADR structure exists here. Create it?", defaultValue: false))
        {
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

    public void Run()
    {
        DisplayInfo();
        var filesInFolder = Directory.GetFiles(_docsFolder, "*.md");
        _indexFile = filesInFolder.FirstOrDefault(f => f.Contains("0000-index"));

        if (string.IsNullOrEmpty(_indexFile))
        {
            Cout.Fail("Ubable to locate index file in '{Path}'", _docsFolder);
            return;
        }

        var entries = IndexManipulator.GetEntriesFromIndexFile(_indexFile);

        if (entries.Any())
        {
            new LiveDataTable<AdrEntry>()
                .WithHeader($"There are {entries.Count} entries in this ADR")
                .WithDataSource(entries)
                .WithColumns("Id", "Title")
                .WithDataPicker(e => new List<string> { e.Number.ToString(), e.Title })
                .WithEnterInstruction("Open entry #{0} with VS Code", p => p.Number.ToString())
                .WithMultipleActions(new[]
                {
                    new LiveKeyAction<AdrEntry>('a', "Append new document", _ => AppendNew(entries)),
                    new LiveKeyAction<AdrEntry>('r', "Rename", entry => Rename(entry, entries)),
                    new LiveKeyAction<AdrEntry>('i', "Rebuild index from folder content", _ => IndexManipulator.RecreateIndex(_docsFolder))
                })
                .WithSelectionAction(entry => VSCode.OpenFile(Path.Combine(_docsFolder, entry.Url)))
                .Start();
        }
    }

    private void Rename(AdrEntry entry, List<AdrEntry> entries)
    {
        var originalTitle = entry.Title;
        var originalUrl = entry.Url;
        var originalFilePath = Path.Combine(_docsFolder, originalUrl);

        var newTitle = AnsiConsole.Ask<string>("New title: ");
        entry.Title = newTitle;
        entry.Url = $"./{entry.Number:0000}-{newTitle.Replace(" ", "-").ToLower()}.md";
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
        var newFileName = $"{nextNumber:0000}-{newName.Replace(" ", "-").ToLower()}.md";
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

    private void DisplayInfo()
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
