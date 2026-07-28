namespace Dentists.Application.Exceptions;

/// <summary>
/// Base class for expected, business rule violations. These are translated into
/// a meaningful HTTP response by the global exception handler instead of a 500.
/// </summary>
public abstract class BusinessException : Exception
{
    protected BusinessException(string title, string detail)
        : base(detail)
    {
        Title = title;
    }

    /// <summary>
    /// Short, human readable summary of the violated rule.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// HTTP status code the API should return for this violation.
    /// </summary>
    public abstract int StatusCode { get; }
}
