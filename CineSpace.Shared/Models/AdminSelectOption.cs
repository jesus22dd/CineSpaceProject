namespace CineSpace.Shared.Models;

public sealed record AdminSelectOption(string Value, string Label)
{
    public static IReadOnlyList<AdminSelectOption> From(IEnumerable<string> values) => values.Select(value => new AdminSelectOption(value, value)).ToList();
}
