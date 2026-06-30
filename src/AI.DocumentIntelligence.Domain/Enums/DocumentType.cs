namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Supported source document formats for upload and processing.</summary>
public enum DocumentType
{
    /// <summary>Portable Document Format (<c>.pdf</c>).</summary>
    Pdf = 0,

    /// <summary>Microsoft Word (<c>.docx</c>).</summary>
    Docx = 1,

    /// <summary>Plain text (<c>.txt</c>).</summary>
    Txt = 2,

    /// <summary>Comma-separated values (<c>.csv</c>).</summary>
    Csv = 3,

    /// <summary>Microsoft PowerPoint (<c>.pptx</c>).</summary>
    Pptx = 4,
}
