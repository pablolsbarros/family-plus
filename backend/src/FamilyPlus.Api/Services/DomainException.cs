namespace FamilyPlus.Api.Services;

public sealed class DomainException(string message, params string[] errors) : Exception(message)
{
    public IReadOnlyList<string> Errors { get; } = errors.Length == 0 ? [message] : errors;
}
