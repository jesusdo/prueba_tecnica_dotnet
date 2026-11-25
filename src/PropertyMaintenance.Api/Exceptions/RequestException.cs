namespace PropertyMaintenance.Api.Exceptions;

public sealed class RequestException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
