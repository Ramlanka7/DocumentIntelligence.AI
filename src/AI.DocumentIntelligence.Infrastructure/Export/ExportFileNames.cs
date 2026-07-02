namespace AI.DocumentIntelligence.Infrastructure.Export;

/// <summary>
/// Utility that produces sanitized, URL-safe file names for exported documents.
/// </summary>
internal static class ExportFileNames
{
    /// <summary>
    /// Generates a sanitized download filename in the form
    /// <c>{slug}-{kind}.{extension}</c> where <paramref name="title"/> is slugified.
    /// </summary>
    public static string Generate(string title, string kind, string extension)
    {
        var slug = Slugify(title);
        return string.IsNullOrEmpty(slug)
            ? $"export-{kind}.{extension}"
            : $"{slug}-{kind}.{extension}";
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new char[value.Length];
        var idx = 0;
        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars[idx++] = ch;
            }
            else if (ch is ' ' or '_' or '-')
            {
                chars[idx++] = '-';
            }
            // else skip
        }

        var slug = new string(chars, 0, idx).Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Length > 80 ? slug[..80] : slug;
    }
}
