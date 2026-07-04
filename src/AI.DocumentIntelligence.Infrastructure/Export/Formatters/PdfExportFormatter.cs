using System.Text;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;

namespace AI.DocumentIntelligence.Infrastructure.Export.Formatters;

/// <summary>
/// Exports analysis and comparison results as PDF documents using a minimal, zero-dependency
/// PDF 1.4 writer. Standard Type1 fonts (Helvetica/Helvetica-Bold) are used — they are built
/// into every PDF viewer so no font embedding is needed, and there are no native-library or
/// globalization requirements. Citations are included inline and in a numbered Sources section.
/// </summary>
internal sealed class PdfExportFormatter : IExportFormatter
{
    public ExportFormat Format => ExportFormat.Pdf;

    public ExportDocumentResult FormatAnalysis(AnalysisResult result, string title)
    {
        var writer = new PdfDocumentWriter();

        writer.AddBlock(PdfBlock.Title(title));
        writer.AddBlock(PdfBlock.Subtitle("Document Intelligence Analysis Report"));
        writer.AddBlock(PdfBlock.Rule());

        writer.AddBlock(PdfBlock.H1("Executive Summary"));
        writer.AddBlock(PdfBlock.Body(result.ExecutiveSummary));

        if (result.KeyFindings.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Key Findings"));
            foreach (var f in result.KeyFindings)
            {
                writer.AddBlock(PdfBlock.H2(f.Title));
                writer.AddBlock(PdfBlock.Body(f.Detail));
                foreach (var c in f.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Risks.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Risks"));
            foreach (var r in result.Risks)
            {
                writer.AddBlock(PdfBlock.H2($"{r.Title}  [Severity: {r.Severity}]"));
                writer.AddBlock(PdfBlock.Body(r.Description));
                foreach (var c in r.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Recommendations.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Recommendations"));
            foreach (var r in result.Recommendations)
            {
                writer.AddBlock(PdfBlock.H2(r.Title));
                writer.AddBlock(PdfBlock.Body(r.Detail));
                foreach (var c in r.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.ActionItems.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Action Items"));
            foreach (var item in result.ActionItems)
            {
                var ownerSuffix = string.IsNullOrWhiteSpace(item.Owner)
                    ? string.Empty
                    : $"  (Owner: {item.Owner})";
                writer.AddBlock(PdfBlock.Bullet(item.Description + ownerSuffix));
                foreach (var c in item.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Sources.Count > 0)
        {
            writer.AddBlock(PdfBlock.Rule());
            writer.AddBlock(PdfBlock.H1("Sources"));
            for (var i = 0; i < result.Sources.Count; i++)
            {
                writer.AddBlock(PdfBlock.Body($"[{i + 1}] {FormatFullCitation(result.Sources[i])}"));
            }
        }

        return Build(writer, title, "analysis");
    }

    public ExportDocumentResult FormatComparison(ComparisonResult result, string title)
    {
        var writer = new PdfDocumentWriter();

        writer.AddBlock(PdfBlock.Title(title));
        writer.AddBlock(PdfBlock.Subtitle("Document Intelligence Comparison Report"));
        writer.AddBlock(PdfBlock.Rule());

        writer.AddBlock(PdfBlock.H1("Executive Overview"));
        writer.AddBlock(PdfBlock.Body(result.ExecutiveOverview));

        if (result.Differences.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Change Log"));
            foreach (var d in result.Differences)
            {
                writer.AddBlock(PdfBlock.H2($"[{d.Type}] {d.Section}"));
                writer.AddBlock(PdfBlock.Body(d.Summary));
                if (!string.IsNullOrWhiteSpace(d.Before))
                {
                    writer.AddBlock(PdfBlock.Body($"Before: {d.Before}"));
                }

                if (!string.IsNullOrWhiteSpace(d.After))
                {
                    writer.AddBlock(PdfBlock.Body($"After:  {d.After}"));
                }

                foreach (var c in d.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Risks.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Risks"));
            foreach (var r in result.Risks)
            {
                writer.AddBlock(PdfBlock.H2($"{r.Title}  [Severity: {r.Severity}]"));
                writer.AddBlock(PdfBlock.Body(r.Description));
                foreach (var c in r.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Recommendations.Count > 0)
        {
            writer.AddBlock(PdfBlock.H1("Recommendations"));
            foreach (var r in result.Recommendations)
            {
                writer.AddBlock(PdfBlock.H2(r.Title));
                writer.AddBlock(PdfBlock.Body(r.Detail));
                foreach (var c in r.Citations)
                {
                    writer.AddBlock(PdfBlock.Citation(FormatCitationLine(c)));
                }
            }
        }

        if (result.Sources.Count > 0)
        {
            writer.AddBlock(PdfBlock.Rule());
            writer.AddBlock(PdfBlock.H1("Sources"));
            for (var i = 0; i < result.Sources.Count; i++)
            {
                writer.AddBlock(PdfBlock.Body($"[{i + 1}] {FormatFullCitation(result.Sources[i])}"));
            }
        }

        return Build(writer, title, "comparison");
    }

    private static ExportDocumentResult Build(PdfDocumentWriter writer, string title, string kind) =>
        new(writer.GeneratePdf(), "application/pdf", ExportFileNames.Generate(title, kind, "pdf"));

    private static string FormatCitationLine(Citation c) =>
        $"  [{c.DocumentName}, p.{c.PageNumber}, {c.ParagraphReference}, conf: {c.ConfidenceScore:P0}]";

    private static string FormatFullCitation(Citation c) =>
        $"{c.DocumentName}, p.{c.PageNumber}, {c.ParagraphReference} — " +
        $"confidence: {c.ConfidenceScore:P0}  \"{c.Snippet}\"";
}

// ── Minimal PDF 1.4 writer (no external dependencies) ─────────────────────────────────────────

/// <summary>Represents a single styled content block added to the PDF document.</summary>
internal sealed record PdfBlock(PdfBlockKind Kind, string Text)
{
    public static PdfBlock Title(string t) => new(PdfBlockKind.Title, t);
    public static PdfBlock Subtitle(string t) => new(PdfBlockKind.Subtitle, t);
    public static PdfBlock H1(string t) => new(PdfBlockKind.H1, t);
    public static PdfBlock H2(string t) => new(PdfBlockKind.H2, t);
    public static PdfBlock Body(string t) => new(PdfBlockKind.Body, t);
    public static PdfBlock Bullet(string t) => new(PdfBlockKind.Bullet, t);
    public static PdfBlock Citation(string t) => new(PdfBlockKind.Citation, t);
    public static PdfBlock Rule() => new(PdfBlockKind.Rule, string.Empty);
}

internal enum PdfBlockKind { Title, Subtitle, H1, H2, Body, Bullet, Citation, Rule }

/// <summary>
/// Builds a valid PDF 1.4 byte array from a sequence of <see cref="PdfBlock"/> items using only
/// standard library types. Supports A4 pages, two fonts, automatic line wrapping and page breaks,
/// and a footer with page numbers.
/// </summary>
internal sealed class PdfDocumentWriter
{
    // A4 dimensions in points (1pt = 1/72 inch)
    private const float PageW = 595.28f;
    private const float PageH = 841.89f;
    private const float MarginL = 56f;   // ~2 cm
    private const float MarginR = 56f;
    private const float MarginT = 72f;
    private const float MarginB = 72f;

    private static readonly float TextW = PageW - MarginL - MarginR;

    // Per-block-kind metrics: (fontSize, leading, avgCharWidth, bold, indentLeft)
    // avgCharWidth is tuned for Helvetica; close enough for word-wrap purposes.
    private static readonly (float Size, float Leading, float AvgCharW, bool Bold, float IndentL)[] Metrics =
    [
        /* Title    */ (22f, 30f, 12.5f, true,  0f),
        /* Subtitle */ (13f, 18f,  7.5f, false, 0f),
        /* H1       */ (16f, 24f,  9.0f, true,  0f),
        /* H2       */ (13f, 20f,  7.5f, true,  0f),
        /* Body     */ (11f, 15f,  6.1f, false, 0f),
        /* Bullet   */ (11f, 15f,  6.1f, false, 14f),
        /* Citation */ ( 9f, 13f,  5.0f, false, 20f),
        /* Rule     */ ( 0f,  8f,  0f,   false, 0f),
    ];

    private readonly List<PdfBlock> _blocks = [];

    public void AddBlock(PdfBlock block) => _blocks.Add(block);

    // ---- PDF generation -----------------------------------------------------------------

    public byte[] GeneratePdf()
    {
        var commands = Layout();
        var pageCount = commands.Select(c => c.Page).DefaultIfEmpty(0).Max() + 1;

        var buf = new List<byte>(64 * 1024);
        var offsets = new List<int>(); // byte offset of each PDF object

        AppendRaw(buf, "%PDF-1.4\n%\x80\x81\x82\x83\n");

        // Object 1 — Catalog
        offsets.Add(0); // index 0 is unused (free object)
        offsets.Add(buf.Count);
        AppendRaw(buf, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n\n");

        // Object 2 — Pages
        offsets.Add(buf.Count);
        var pageObjIds = Enumerable.Range(3, pageCount).ToArray();
        var kidsStr = string.Join(" ", pageObjIds.Select(n => $"{n} 0 R"));
        AppendRaw(buf, $"2 0 obj\n<< /Type /Pages /Kids [{kidsStr}] /Count {pageCount} >>\nendobj\n\n");

        // Font objects come after page content pairs
        // Layout: page 0 obj = 3, content 0 = 4, page 1 obj = 5, content 1 = 6, ...
        // Font F1 = 3 + 2*pageCount, Font F2 = 4 + 2*pageCount
        int fontF1Id = 3 + 2 * pageCount;
        int fontF2Id = 4 + 2 * pageCount;

        var pageStreams = BuildPageContentStreams(commands, pageCount);

        for (var p = 0; p < pageCount; p++)
        {
            int pageObjId = 3 + 2 * p;
            int contentObjId = 4 + 2 * p;

            offsets.Add(buf.Count);
            AppendRaw(buf,
                $"{pageObjId} 0 obj\n" +
                $"<< /Type /Page /Parent 2 0 R " +
                $"/MediaBox [0 0 {PageW:F2} {PageH:F2}] " +
                $"/Contents {contentObjId} 0 R " +
                $"/Resources << /Font << /F1 {fontF1Id} 0 R /F2 {fontF2Id} 0 R >> >> >>\n" +
                $"endobj\n\n");

            var streamBytes = Encoding.Latin1.GetBytes(pageStreams[p]);
            offsets.Add(buf.Count);
            AppendRaw(buf, $"{contentObjId} 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
            buf.AddRange(streamBytes);
            AppendRaw(buf, "\nendstream\nendobj\n\n");
        }

        // Font objects
        offsets.Add(buf.Count);
        AppendRaw(buf,
            $"{fontF1Id} 0 obj\n" +
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\n" +
            "endobj\n\n");

        offsets.Add(buf.Count);
        AppendRaw(buf,
            $"{fontF2Id} 0 obj\n" +
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\n" +
            "endobj\n\n");

        // xref table
        int xrefOffset = buf.Count;
        int totalObjs = 4 + 2 * pageCount; // catalog + pages + N*(page+content) + 2 fonts
        AppendRaw(buf, $"xref\n0 {totalObjs + 1}\n");
        AppendRaw(buf, "0000000000 65535 f \n");
        for (var i = 1; i <= totalObjs; i++)
        {
            var off = i < offsets.Count ? offsets[i] : 0;
            AppendRaw(buf, $"{off:D10} 00000 n \n");
        }

        AppendRaw(buf,
            $"trailer\n<< /Size {totalObjs + 1} /Root 1 0 R >>\n" +
            $"startxref\n{xrefOffset}\n%%EOF\n");

        return [.. buf];
    }

    // ── Layout ──────────────────────────────────────────────────────────────────────────────

    private sealed record PrintCmd(int Page, float Y, string Text, float Size, bool Bold, float X);

    private List<PrintCmd> Layout()
    {
        var cmds = new List<PrintCmd>();
        var page = 0;
        var y = PageH - MarginT; // PDF y-axis: origin bottom-left, text starts near top

        foreach (var block in _blocks)
        {
            var (size, leading, avgW, bold, indentL) = Metrics[(int)block.Kind];

            if (block.Kind == PdfBlockKind.Rule)
            {
                y -= leading;
                if (y < MarginB)
                {
                    page++;
                    y = PageH - MarginT;
                }

                cmds.Add(new PrintCmd(page, y, "---RULE---", 0, false, MarginL));
                y -= leading;
                continue;
            }

            if (string.IsNullOrWhiteSpace(block.Text))
            {
                y -= leading;
                if (y < MarginB)
                {
                    page++;
                    y = PageH - MarginT;
                }

                continue;
            }

            float lineW = TextW - indentL;
            int maxChars = Math.Max(1, (int)(lineW / avgW));
            var lines = WrapText(block.Text, maxChars);

            if (block.Kind is PdfBlockKind.H1 or PdfBlockKind.Title)
            {
                y -= 6f;
            }

            foreach (var line in lines)
            {
                if (y < MarginB)
                {
                    page++;
                    y = PageH - MarginT;
                }

                cmds.Add(new PrintCmd(page, y, line, size, bold, MarginL + indentL));
                y -= leading;
            }

            if (block.Kind is PdfBlockKind.H1 or PdfBlockKind.Title)
            {
                y -= 4f;
            }
        }

        return cmds;
    }

    // ── Content stream builder ───────────────────────────────────────────────────────────────

    private static List<string> BuildPageContentStreams(List<PrintCmd> commands, int pageCount)
    {
        var streams = new string[pageCount];

        for (var p = 0; p < pageCount; p++)
        {
            var sb = new StringBuilder();
            var pageCmds = commands.Where(c => c.Page == p).ToList();

            // Horizontal rules (drawn outside BT block)
            foreach (var cmd in pageCmds.Where(c => c.Text == "---RULE---"))
            {
                sb.AppendLine("q");
                sb.AppendLine("0.7 0.7 0.7 RG");
                sb.AppendLine($"{MarginL:F2} {cmd.Y:F2} m");
                sb.AppendLine($"{PageW - MarginR:F2} {cmd.Y:F2} l");
                sb.AppendLine("S");
                sb.AppendLine("Q");
            }

            // Text commands
            var textCmds = pageCmds.Where(c => c.Text != "---RULE---").ToList();
            if (textCmds.Count > 0)
            {
                sb.AppendLine("BT");
                var curFont = string.Empty;

                foreach (var cmd in textCmds)
                {
                    var fontName = cmd.Bold ? "F2" : "F1";
                    var fontKey = $"{fontName}_{cmd.Size:F1}";
                    if (fontKey != curFont)
                    {
                        sb.AppendLine($"/{fontName} {cmd.Size:F1} Tf");
                        curFont = fontKey;
                    }

                    sb.AppendLine($"{cmd.X:F2} {cmd.Y:F2} Td");
                    sb.AppendLine($"({PdfEscape(cmd.Text)}) Tj");
                    sb.AppendLine($"{-cmd.X:F2} {-cmd.Y:F2} Td");
                }

                sb.AppendLine("ET");
            }

            // Footer
            sb.AppendLine("BT");
            sb.AppendLine($"/F1 9 Tf");
            sb.AppendLine($"{MarginL:F2} {(MarginB - 18):F2} Td");
            sb.AppendLine($"(Page {p + 1} of {pageCount}   |   Generated by Document Intelligence Platform) Tj");
            sb.AppendLine("ET");

            streams[p] = sb.ToString();
        }

        return [.. streams];
    }

    // ── Utilities ────────────────────────────────────────────────────────────────────────────

    private static List<string> WrapText(string text, int maxChars)
    {
        var lines = new List<string>();
        var paragraphs = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();

            foreach (var word in words)
            {
                var sanitised = SanitiseForPdf(word);
                if (current.Length == 0)
                {
                    current.Append(sanitised);
                }
                else if (current.Length + 1 + sanitised.Length <= maxChars)
                {
                    current.Append(' ');
                    current.Append(sanitised);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(sanitised);
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }
        }

        return lines.Count > 0 ? lines : [string.Empty];
    }

    /// <summary>
    /// Converts a string to Latin-1 (WinAnsiEncoding) by replacing characters that cannot be
    /// represented with close ASCII equivalents, then escapes PDF string delimiters.
    /// </summary>
    private static string SanitiseForPdf(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch < 128)
            {
                sb.Append(ch);
            }
            else if (ch <= 255)
            {
                sb.Append(ch);
            }
            else
            {
                switch (ch)
                {
                    case '\u2018' or '\u2019':
                        sb.Append('\'');
                        break;
                    case '\u201C' or '\u201D':
                        sb.Append('"');
                        break;
                    case '\u2013' or '\u2014':
                        sb.Append('-');
                        break;
                    case '\u2026':
                        sb.Append("...");
                        break;
                    case '\u2022' or '\u00B7':
                        sb.Append('*');
                        break;
                    case '\u00AE':
                        sb.Append("(R)");
                        break;
                    case '\u2122':
                        sb.Append("(TM)");
                        break;
                    default:
                        sb.Append('?');
                        break;
                }
            }
        }

        return sb.ToString();
    }

    private static string PdfEscape(string s)
    {
        return s
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static void AppendRaw(List<byte> buf, string s)
    {
        buf.AddRange(Encoding.Latin1.GetBytes(s));
    }
}
