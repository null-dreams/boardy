namespace Boardy.Application.Common.Results;

public sealed record Error
{
    public string Code { get; }
    public string Description { get; }

    public Error(string code, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Code = code;
        Description = description;
    }
}
