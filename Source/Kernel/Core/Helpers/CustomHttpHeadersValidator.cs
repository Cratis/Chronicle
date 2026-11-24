// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Chronicle.Helpers;

/// <summary>
/// Validates custom HTTP headers using the same parser as delivery.
/// </summary>
internal class CustomHttpHeadersValidator : AbstractValidator<IDictionary<string, string>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomHttpHeadersValidator"/> class.
    /// </summary>
    public CustomHttpHeadersValidator()
    {
        RuleForEach(_ => _).Custom((header, context) =>
        {
            using var request = new HttpRequestMessage();
            try
            {
                CustomHttpHeaders.Add(request.Headers, header.Key, header.Value);
            }
            catch (InvalidCustomHttpHeader exception)
            {
                context.AddFailure(exception.Message);
            }
        });
    }
}
