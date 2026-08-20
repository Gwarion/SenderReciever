namespace Sender.Api;

sealed class UploadLineFormatter
{
    public const int UploadedLineLength = 65;

    /// <summary>
    /// Formats the two record fields required by the upload file.
    /// </summary>
    /// <param name="record">Database record containing all simulated fields.</param>
    /// <returns>A line payload without the trailing newline.</returns>
    public string FormatLine(DatabaseRecord record) =>
        string.Concat(record.Field1.ToString("N"), record.Field2.ToString("N"));
}
