namespace Adr.Models;

public record AdrEntry
{
    public int Number { get; init; }
    public required string Title { get; set; }
    public required string FilePath { get; set; }
    public int SupersededBy { get; set; }
}
