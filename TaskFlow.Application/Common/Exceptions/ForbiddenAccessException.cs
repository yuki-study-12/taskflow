namespace TaskFlow.Application.Common.Exceptions;

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Access to this resource is forbidden.")
    { }
}
