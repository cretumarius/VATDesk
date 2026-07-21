using System.Text;

namespace VatDesk.Infrastructure.Parsing;

/// <summary>Format detection: a file whose first non-whitespace character is '&lt;' is XML; otherwise CSV.</summary>
internal static class ContentSniffing
{
    public static bool FirstNonWhitespaceCharIsLessThan(Stream content)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("Content stream must be seekable for format sniffing.", nameof(content));
        }

        var originalPosition = content.Position;
        try
        {
            content.Position = 0;
            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            int ch;
            while ((ch = reader.Read()) != -1)
            {
                if (!char.IsWhiteSpace((char)ch))
                {
                    return (char)ch == '<';
                }
            }

            return false;
        }
        finally
        {
            content.Position = originalPosition;
        }
    }
}
