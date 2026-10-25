// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents the validator for <see cref="RegisterEventSources"/>.
/// </summary>
internal class RegisterEventSourcesValidator : CommandValidator<RegisterEventSources>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterEventSourcesValidator"/> class.
    /// </summary>
    public RegisterEventSourcesValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Sources).NotNull().WithMessage("Event source definitions are required.");
        RuleFor(_ => _.Sources).Custom((sources, context) =>
        {
            if (sources is null)
            {
                return;
            }

            var duplicates = sources
                .Where(_ => !string.IsNullOrWhiteSpace(_.Name))
                .GroupBy(_ => _.Name, StringComparer.Ordinal)
                .Where(_ => _.Count() > 1);
            foreach (var duplicate in duplicates)
            {
                context.AddFailure(nameof(RegisterEventSources.Sources), $"The event source name '{duplicate.Key}' is registered more than once.");
            }
        });
        RuleForEach(_ => _.Sources).ChildRules(source =>
        {
            source.RuleFor(_ => _.Name).NotEmpty().WithMessage("Event source name is required.");
            source.RuleFor(_ => _).Custom((definition, context) =>
            {
                var duplicates = (definition.Streams ?? [])
                    .Where(_ => !string.IsNullOrWhiteSpace(_.Name))
                    .GroupBy(_ => _.Name, StringComparer.Ordinal)
                    .Where(_ => _.Count() > 1);
                foreach (var duplicate in duplicates)
                {
                    context.AddFailure(nameof(RegisterEventSources.Sources), $"The event source '{definition.Name}' declares the event stream '{duplicate.Key}' more than once.");
                }
            });
            source.RuleForEach(_ => _.Streams).ChildRules(stream =>
                stream.RuleFor(_ => _.Name).NotEmpty().WithMessage("Event stream name is required."));
        });
    }
}
