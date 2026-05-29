using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AcoTestApi.Application.Common.Exceptions;

namespace AcoTestApi.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults =
                await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                throw new CustomValidationException(HttpStatusCode.BadRequest,
                    failures.Select(f => new ValidationErrorDetail(
                            ToCamelCaseFieldPath(f.PropertyName),
                            f.ErrorMessage))
                        .ToList());
            }
        }

        return await next();
    }

    private static string ToCamelCaseFieldPath(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        var segments = propertyName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            segments[i] = char.ToLowerInvariant(segment[0]) + segment[1..];
        }

        return string.Join(".", segments);
    }
}
