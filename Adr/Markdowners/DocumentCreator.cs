using Adr.Models;

namespace Adr.Markdowners;

public static class DocumentCreator
{
    private const string InitialIndexContent = """
            # Index

            | Number | Title | Superseded by |
            | ------ | ----- | ------------- |
            | 1 | [Record Architecture Decisions](./0001-record-architecture-decisions.md)| |
            """;

    public static void CreateFirstEntry(string docsPath)
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

        File.WriteAllText(Path.Combine(docsPath, "0000-index.md"), InitialIndexContent);
        Cout.Success("Created '{FileName}'", "0000-index.md");

        File.WriteAllText(Path.Combine(docsPath, "0001-record-architecture-decisions.md"), recordAdrs);
        Cout.Success("Created '{FileName}'", "0001-record-architecture-decisions.md");
    }

    public static string Create(string docsPath, AdrEntry newEntry)
    {
        if (!Directory.Exists(docsPath))
        {
            Cout.Fail("Yeah, see, the path '{Path}' doesen't exist!", docsPath);
            return string.Empty;
        }

        var filePath = Path.Combine(docsPath, newEntry.Url);

        var documentContent = $$"""
            # 1. {{newEntry.Title}}

            {{DateTime.Now:yyyy-MM-dd}}

            ## Status

            Proposed

            ## Context

            {context}

            ## Decision

            {decision}

            ## Consequences

            {consequences}
            """;

        if (File.Exists(filePath))
        {
            Cout.Fail("Oops! '{DocPath}' already exists!", filePath);
            return string.Empty;
        }

        File.WriteAllText(filePath, documentContent);
        return filePath;
    }
}
