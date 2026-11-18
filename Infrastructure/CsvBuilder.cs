using System.Text;

namespace DigitalFarmManagement.Infrastructure;

public static class CsvBuilder
{
    public static byte[] Build(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
            builder.AppendLine(string.Join(",", row.Select(Escape)));

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(builder.ToString());
        var file = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, file, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, file, preamble.Length, body.Length);
        return file;
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && "=+-@".Contains(text[0]))
            text = "'" + text;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }
}
