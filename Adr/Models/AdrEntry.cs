namespace Adr.Models;

public record AdrEntry
{
    public int Number { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
}
