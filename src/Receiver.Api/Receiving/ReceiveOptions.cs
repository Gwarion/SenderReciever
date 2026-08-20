namespace Receiver.Api;

sealed record ReceiveOptions(string OutputDirectory, string OutputFileName, int FlushEveryLines, int BufferSizeBytes)
{
    /// <summary>
    /// Reads receiver options from the request query string and application configuration.
    /// </summary>
    /// <param name="query">Request query values.</param>
    /// <param name="configuration">Application configuration used for output directory defaults.</param>
    /// <returns>Validated receiver options.</returns>
    public static ReceiveOptions From(IQueryCollection query, IConfiguration configuration)
    {
        var outputDirectory = query.TryGetValue("outputDirectory", out var output)
            ? output.ToString()
            : configuration["Receiver:OutputDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "received");
        var outputFileName = query.TryGetValue("outputFileName", out var fileName)
            ? fileName.ToString()
            : configuration["Receiver:OutputFileName"] ?? "received.txt";
        var flushEveryLines = ReadInt(query, "flushEveryLines", 200_000);
        var bufferSizeBytes = ReadInt(query, "bufferSizeBytes", 1024 * 1024);

        return new(
            outputDirectory,
            Path.GetFileName(outputFileName),
            Math.Clamp(flushEveryLines, 1, 200_000),
            Math.Clamp(bufferSizeBytes, 4 * 1024, 16 * 1024 * 1024));
    }

    static int ReadInt(IQueryCollection query, string key, int fallback) =>
        query.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
}
