using System.Text;

namespace DigitalFarmManagement.Infrastructure;

public static class PdfReportBuilder
{
    public static byte[] Build(string title, string subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        const int rowsPerPage = 16;
        var pageCount = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)rowsPerPage));
        var streams = new List<string>();
        for (var page = 0; page < pageCount; page++)
        {
            var slice = rows.Skip(page * rowsPerPage).Take(rowsPerPage).ToList();
            streams.Add(RenderPage(title, subtitle, headers, slice, page + 1, pageCount));
        }

        return Assemble(streams);
    }

    private static string RenderPage(string title, string subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, int page, int pageCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("0.078 0.263 0.173 rg");
        sb.AppendLine("0 548 842 47 re f");
        sb.AppendLine("1 1 1 rg");
        Text(sb, 36, 572, 16, Clean(title, 80));
        Text(sb, 36, 556, 9, Clean(subtitle, 110));
        sb.AppendLine("0.15 0.2 0.16 rg");

        var columns = Math.Max(1, headers.Count);
        var left = 32f;
        var width = 778f / columns;
        var y = 520f;
        for (var c = 0; c < headers.Count; c++)
            Text(sb, left + (c * width), y, 8, Clean(headers[c], ColumnChars(width)));

        sb.AppendLine("0.75 0.78 0.76 RG");
        sb.AppendLine($"32 {y - 6:0.##} m 810 {y - 6:0.##} l S");
        sb.AppendLine("0 0 0 rg");

        y = 496;
        foreach (var row in rows)
        {
            for (var c = 0; c < headers.Count; c++)
            {
                var cell = c < row.Count ? row[c] : string.Empty;
                Text(sb, left + (c * width), y, 8, Clean(cell, ColumnChars(width)));
            }
            y -= 24;
        }

        sb.AppendLine("0.35 0.4 0.37 rg");
        Text(sb, 32, 28, 8, Clean($"AgriGuard  ·  Page {page} of {pageCount}", 60));
        return sb.ToString();
    }

    private static int ColumnChars(float width) => Math.Max(8, (int)(width / 4.6f));

    private static void Text(StringBuilder sb, float x, float y, int size, string text)
    {
        sb.AppendLine($"BT /F1 {size} Tf {x:0.##} {y:0.##} Td ({text}) Tj ET");
    }

    private static string Clean(string? value, int max)
    {
        var text = value ?? string.Empty;
        var chars = text.Select(c => c switch
        {
            '(' or ')' or '\\' => ' ',
            _ when c is 'µ' or 'μ' => 'u',
            _ when c < 32 || c > 126 => ' ',
            _ => c
        }).ToArray();
        var cleaned = new string(chars).Trim();
        if (cleaned.Length > max)
            cleaned = cleaned[..Math.Max(1, max - 1)] + ".";
        return cleaned;
    }

    private static byte[] Assemble(List<string> streams)
    {
        using var ms = new MemoryStream();
        void Write(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            ms.Write(bytes, 0, bytes.Length);
        }

        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        var next = 1;

        long Add(string body)
        {
            var position = ms.Position;
            offsets.Add(position);
            Write($"{next} 0 obj\n{body}\nendobj\n");
            next++;
            return position;
        }

        Add("<< /Type /Catalog /Pages 2 0 R >>");

        var pageObjectNumbers = new List<int>();
        var fontObject = 3;
        var cursor = 4;
        for (var i = 0; i < streams.Count; i++)
        {
            pageObjectNumbers.Add(cursor);
            cursor += 2;
        }

        var kids = string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"));
        Add($"<< /Type /Pages /Kids [{kids}] /Count {streams.Count} >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        foreach (var stream in streams)
        {
            var length = Encoding.ASCII.GetByteCount(stream);
            var pageNumber = next;
            var contentNumber = next + 1;
            Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Contents {contentNumber} 0 R /Resources << /Font << /F1 {fontObject} 0 R >> >> >>");
            Add($"<< /Length {length} >>\nstream\n{stream}endstream");
            _ = pageNumber;
        }

        var xref = ms.Position;
        Write($"xref\n0 {next}\n");
        Write("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            Write($"{offsets[i]:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {next} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }
}
