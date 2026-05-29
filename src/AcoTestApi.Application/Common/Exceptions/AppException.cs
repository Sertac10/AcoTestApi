using System;
using System.Net;

namespace AcoTestApi.Application.Common.Exceptions;

public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ErrorMessage { get; }

    public AppException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorMessage = message;
    }

    public HttpStatusCode GetStatusCode() => StatusCode;
    public string GetErrorMessage() => ErrorMessage;
}
