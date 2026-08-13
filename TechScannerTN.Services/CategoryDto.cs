namespace Hi_Trade.Services;

public sealed record CategoryDto(
    string ParentCategory,
    string Title,
    string Url);
