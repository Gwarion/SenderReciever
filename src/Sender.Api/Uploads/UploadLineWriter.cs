using System.Text;

namespace Sender.Api;

sealed class UploadLineWriter
{
    public const int UploadedLineLength = 65;

    /// <summary>
    /// Writes the two GUID fields required by the upload file into a reusable line buffer.
    /// </summary>
    /// <param name="record">Database record containing all simulated fields.</param>
    /// <param name="line">Reusable destination buffer. Must be at least 65 bytes.</param>
    public void WriteUploadLine(DatabaseRecord record, byte[] line)
    {
        Span<char> chars = stackalloc char[64];
        record.Field1.TryFormat(chars[..32], out _, "N");
        record.Field2.TryFormat(chars[32..64], out _, "N");
        Encoding.ASCII.GetBytes(chars, line.AsSpan(0, 64));
    }
}
