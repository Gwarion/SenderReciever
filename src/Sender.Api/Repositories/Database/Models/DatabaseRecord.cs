namespace Sender.Api;

public readonly record struct DatabaseRecord(
    Guid Field1,
    Guid Field2,
    Guid Field3,
    Guid Field4,
    Guid Field5,
    Guid Field6)
{
    public static DatabaseRecord Create() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());
}
