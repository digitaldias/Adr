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

public sealed partial class IndexManipulator
{
    public static void RecreateIndex(string docsFolder)
    {
        var freshEntries = CreateEntryListFromDocsFolder(docsFolder);
        if (freshEntries is null)
        {
            return;
        }

        var content = CreateIndexFileContent(freshEntries);

        var filePath = Path.Combine(docsFolder, "0000-index.md");
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public static string CreateIndexFromAdrEntries(SortedList<int, AdrEntry> entries, string docsFolder)
    {
        var content = CreateIndexFileContent(entries);
        var filePath = Path.Combine(docsFolder, "0000-index.md");
        File.WriteAllText(filePath, content, Encoding.UTF8);

        return filePath;
    }

    public static string CreateIndexFileContent(SortedList<int, AdrEntry> entries)
    {
        var widestNumber = entries.Max(e => e.Value.Number.ToString(CultureInfo.InvariantCulture).Length);
        var widestTitle = entries.Max(e => e.Value.Title.Length + e.Value.FilePath.Length);

        if (widestNumber < 6)
        {
            widestNumber = 6;
        }

        var tableHeader = $"| Number | {"Title".PadRight(widestTitle + 4)} | Superseded by |";
        var tableLines = $"| {new string('-', widestNumber)} | {new string('-', widestTitle + 4)} | ------------- |";

        var builder = new StringBuilder(4096);
        builder.AppendLine("# Archtecture Decision Records");
        builder.AppendLine();
        builder.AppendLine(tableHeader);
        builder.AppendLine(tableLines);

        foreach (var entry in entries)
        {
            var number = entry.Value.Number.ToString(CultureInfo.InvariantCulture).PadLeft(widestNumber);
            var title = $"[{entry.Value.Title}]({entry.Value.FilePath})".PadRight(widestTitle + 4);
            var super = entry.Value.SupersededBy > 0
                ? entry.Value.SupersededBy.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            builder
                .Append("| ")
                .Append(number)
                .Append(" | ")
                .Append(title)
                .Append(" | ")
                .Append(super)
                .AppendLine(" |");
        }

        return builder.ToString();
    }

    public static void Rewrite(SortedList<int, AdrEntry> allEntries, string indexFile)
    {
        var content = CreateIndexFileContent(allEntries);
        File.WriteAllText(indexFile, content);
    }

    public static IEnumerable<AdrEntry> ReadAdrEntries(string adrFolder)
    {
        var filesInFolder = Directory.GetFiles(adrFolder, "*.md");
        var indexFile = Array.Find(filesInFolder, f => f.Contains("0000-index"));

        if (string.IsNullOrEmpty(indexFile))
        {
            Cout.Fail("Ubable to locate ADR index in '{Path}'", adrFolder);
            return Enumerable.Empty<AdrEntry>();
        }

        return GetEntriesFromIndexFile(indexFile);
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
                var superseededByString = string.Concat(cells[2].Descendants<LiteralInline>().Select(x => x.Content));
                var superseededBy = string.IsNullOrEmpty(superseededByString) ? 0 : int.Parse(superseededByString, CultureInfo.InvariantCulture);

                adrEntries.Add(new()
                {
                    Number = int.Parse(number, CultureInfo.InvariantCulture),
                    Title = title,
                    FilePath = link?.Url ?? string.Empty,
                    SupersededBy = superseededBy
                });
            }
        }

        return adrEntries;
    }

    private static SortedList<int, AdrEntry> CreateEntryListFromDocsFolder(string docsFolder)
    {
        var allFiles = Directory.GetFiles(docsFolder, "*.md");
        if (allFiles.Length == 0)
        {
            return new();
        }

        var allEntries = new SortedList<int, AdrEntry>();
        foreach (var file in allFiles)
        {
            var fileInfo = new FileInfo(file);
            var testResult = NumberAndTitlePattern().Match(fileInfo.Name);
            if (!testResult.Success)
            {
                continue;
            }

            var number = int.Parse(testResult.Groups[1].Value, CultureInfo.InvariantCulture);
            var title = testResult.Groups[2].Value;
            title = $"{char.ToUpper(title[0], CultureInfo.InvariantCulture)}{title[1..].Replace("-", " ")}";

            // Skip the index file itself
            if (number == 0 && title.Equals("Index", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            allEntries.Add(number, new AdrEntry
            {
                Number = number,
                Title = title,
                FilePath = $"./{fileInfo.Name}"
            });
        }

        return allEntries;
    }

    [GeneratedRegex("^(\\d+)-(.*).md$")]
    private static partial Regex NumberAndTitlePattern();
}
