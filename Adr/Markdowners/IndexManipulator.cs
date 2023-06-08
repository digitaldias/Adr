using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Adr.Models;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableRow = Markdig.Extensions.Tables.TableRow;

namespace Adr.Markdowners;

public sealed class IndexManipulator
{
    private const string InitialIndexContent = """
            # Index

            | Number | Title | Superseded by |
            | ------ | ----- | ------------- |
            | 1 | [Record Architecture Decisions](./0001-record-architecture-decisions.md)| |
            """;

    public static void RecreateIndex(string docsFolder)
    {
        var freshEntries = CreateEntryListFromDocsFolder(docsFolder);
        if (freshEntries is null)
        {
            return;
        }

        var content = CreateContentFromEntries(freshEntries);

        var filePath = Path.Combine(docsFolder, "0000-index.md");
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public static string CreateContentFromEntries(IEnumerable<AdrEntry> entries)
    {
        var widestNumber = entries.Max(e => e.Number.ToString().Length);
        var widestTitle = entries.Max(e => e.Title.Length + e.Url.Length);

        if (widestNumber < 6)
        {
            widestNumber = 6;
        }

        var tableHeader = $"| Number | {"Title".PadRight(widestTitle + 4)} | Superseded by |";
        var tableLines = $"| ------ | {new string('-', widestTitle + 4)} | ------------- |";

        var builder = new StringBuilder(4096);
        builder.AppendLine("# Archtecture Decision Records");
        builder.AppendLine();
        builder.AppendLine(tableHeader);
        builder.AppendLine(tableLines);

        foreach (var entry in entries)
        {
            var number = entry.Number.ToString().PadLeft(widestNumber);
            var title = $"[{entry.Title}]({entry.Url})".PadRight(widestTitle + 4);
            builder.AppendLine($"| {number} | {title} | |");
        }

        return builder.ToString();
    }

    private static IEnumerable<AdrEntry> CreateEntryListFromDocsFolder(string docsFolder)
    {
        // Match files starting with a number and ending in md. Extract number and title
        const string digitPattern = @"^(\d+)-(.*).md$";

        var allFiles = Directory.GetFiles(docsFolder, "*.md");
        if (!allFiles.Any())
        {
            return Enumerable.Empty<AdrEntry>();
        }

        var allEntries = new List<AdrEntry>();
        foreach (var file in allFiles)
        {
            var fileInfo = new FileInfo(file);
            var testResult = Regex.Match(fileInfo.Name, digitPattern);
            if (!testResult.Success)
            {
                continue;
            }
            var number = int.Parse(testResult.Groups[1].Value, CultureInfo.InvariantCulture);
            var title = testResult.Groups[2].Value;
            title = char.ToUpper(title[0]) + (title[1..]).Replace("-", " ");

            // Skip the index file itself
            if (number == 0 && title.Equals("Index", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            allEntries.Add(new AdrEntry
            {
                Number = number,
                Title = title,
                Url = $"./{fileInfo.Name}"
            });
        }

        return allEntries.OrderBy(file => file.Number);
    }


    public static void Rewrite(List<AdrEntry> allEntries, string indexFile)
    {
        var builder = new StringBuilder(InitialIndexContent);
        builder.AppendLine();
        for (int i = 1; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            var url = entry.Url.StartsWith("./") ? entry.Url : $"./{entry.Url}";
            builder.AppendLine($"| {entry.Number} | [{entry.Title}]({url}) | |");
        }

        builder.AppendLine();
        var content = builder.ToString();
        File.WriteAllText(indexFile, content);
    }

    public static void CreateInitialFolderContent(string adrFolder)
    {
        var recordAdrs = $"""
            # 1. Record Architecture Decisions

            {DateTime.Now:yyyy-MM-dd}

            ## Status

            Accepted

            ## Context

            We need to record the architectural decisions made on this project.

            ## Decision

            We will use Architecture Decision Records, as described by Michael Nygard in this article: http://thinkrelevance.com/blog/2011/11/15/documenting-architecture-decisions

            ## Consequences

            See Michael Nygard's article, linked above.
            """;

        File.WriteAllText(Path.Combine(adrFolder, "0000-index.md"), InitialIndexContent);
        Cout.Success("Wrote file: {FileName}", "0000-index.md");

        File.WriteAllText(Path.Combine(adrFolder, "0001-record-architecture-decisions.md"), recordAdrs);
        Cout.Success("Wrote file: {FileName}", "0001-record-architecture-decisions.md");
    }

    public static List<AdrEntry> GetEntriesFromIndexFile(string indexFile)
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

        return adrEntries;
    }
}
