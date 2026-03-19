namespace FlowR.Mapper;

/// <summary>
/// PascalCase naming convention — the default for .NET.
/// </summary>
public sealed class PascalCaseNamingConvention : INamingConvention
{
    public static readonly PascalCaseNamingConvention Instance = new();
    public string Convert(string sourceName) => sourceName;
}

/// <summary>
/// snake_case to PascalCase convention.
/// E.g., "first_name" -> "FirstName"
/// </summary>
public sealed class SnakeCaseToPascalCaseConvention : INamingConvention
{
    public static readonly SnakeCaseToPascalCaseConvention Instance = new();

    public string Convert(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName)) return sourceName;

        return string.Concat(sourceName
            .Split('_')
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    }
}

/// <summary>
/// camelCase to PascalCase convention.
/// E.g., "firstName" -> "FirstName"
/// </summary>
public sealed class CamelCaseToPascalCaseConvention : INamingConvention
{
    public static readonly CamelCaseToPascalCaseConvention Instance = new();

    public string Convert(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName)) return sourceName;
        return char.ToUpperInvariant(sourceName[0]) + sourceName[1..];
    }
}

/// <summary>
/// PascalCase to camelCase convention.
/// E.g., "FirstName" -> "firstName"
/// </summary>
public sealed class PascalCaseToCamelCaseConvention : INamingConvention
{
    public static readonly PascalCaseToCamelCaseConvention Instance = new();

    public string Convert(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName)) return sourceName;
        return char.ToLowerInvariant(sourceName[0]) + sourceName[1..];
    }
}
