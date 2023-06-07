using Adr.Models;

namespace Adr.Markdowners;

public static class DocumentCreator
{
    public static string Create(string docsPath, AdrEntry newEntry)
    {
        if (!Directory.Exists(docsPath))
        {
            Cout.Fail("Yeah, see, the path '{Path}' doesen't exist!", docsPath);
            return string.Empty;
        }

        var adrDocument = Path.Combine(docsPath, newEntry.Url);

        var template = $$"""
            # 1. {{newEntry.Title}}

            {{DateTime.Now.ToString("yyyy-MM-dd")}}

            ## Status

            Proposed

            ## Context

            {context}

            ## Decision

            {decision}

            ## Consequences

            {consequences}
            """;

        if (File.Exists(adrDocument))
        {
            Cout.Fail("Oops! '{DocPath}' already exists!", adrDocument);
            return string.Empty;
        }

        File.WriteAllText(adrDocument, template);
        return adrDocument;
    }
}
