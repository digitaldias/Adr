using System.ComponentModel.DataAnnotations;
using LibGit2Sharp;

namespace Adr;

public sealed class AdrTool
{
    private string _rootFolder;

    public AdrTool(string givenPath)
    {
        if (!Path.Exists(givenPath))
        {
            Cout.Fail("The given path '{Path}' does not exist", givenPath);

            var message = $"The given path '{givenPath}' is not valid";
            throw new UnmatchedPathException(message);
        }

        _rootFolder = GetGitRootFolder(givenPath);
        if (string.IsNullOrEmpty(_rootFolder))
        {
            Cout.Fail("The provided Path '{ProvidedPath}' is not a git repository", givenPath);

            var message = $"The given path '{givenPath}' is not a git repository";
            throw new ValidationException(message);
        }

        var docsFolder = GetDocsFolder(givenPath);
        Cout.Success("ADR Path found in '{DocsFolder}'. We can begin!", docsFolder);
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
