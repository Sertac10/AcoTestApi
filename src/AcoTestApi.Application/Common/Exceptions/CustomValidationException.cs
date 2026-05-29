using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AcoTestApi.Application.Common.Exceptions
{
    public record ValidationErrorDetail(string Field, string Message);

    public class CustomValidationException : Exception
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _errorMessage;
        private readonly IReadOnlyCollection<ValidationErrorDetail> _validationErrors;

        public CustomValidationException(HttpStatusCode statusCode, string message)
        {
            _statusCode = statusCode;
            _errorMessage = message;
            _validationErrors = Array.Empty<ValidationErrorDetail>();
        }

        public CustomValidationException(HttpStatusCode statusCode, ICollection<string> messages)
        {
            _statusCode = statusCode;
            _errorMessage = string.Join(", ", messages);
            _validationErrors = messages.Select(x => new ValidationErrorDetail(string.Empty, x)).ToList();
        }

        public CustomValidationException(HttpStatusCode statusCode, ICollection<ValidationErrorDetail> validationErrors)
        {
            _statusCode = statusCode;
            _validationErrors = validationErrors.ToList();
            _errorMessage = _validationErrors.Count > 0
                ? string.Join(", ", _validationErrors.Select(x => x.Message))
                : "Validation failed.";
        }

        public string GetErrorMessage() => _errorMessage;
        public HttpStatusCode GetStatusCode() => _statusCode;
        public IReadOnlyCollection<ValidationErrorDetail> GetValidationErrors() => _validationErrors;
    }
}
