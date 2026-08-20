namespace Sender.Api;

sealed class CountingWriteStream : Stream
{
    Stream? inner;

    public long BytesWritten { get; private set; }
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => inner?.CanWrite ?? false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public void Reset(Stream stream)
    {
        inner = stream;
        BytesWritten = 0;
    }

    public override void Flush() => inner!.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        inner!.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner!.Write(buffer, offset, count);
        BytesWritten += count;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await inner!.WriteAsync(buffer, cancellationToken);
        BytesWritten += buffer.Length;
    }
}
