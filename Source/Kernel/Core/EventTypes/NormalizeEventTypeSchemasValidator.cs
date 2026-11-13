// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Validates schema normalization using the event store restrictions of event type creation.
/// </summary>
internal class NormalizeEventTypeSchemasValidator : CommandValidator<NormalizeEventTypeSchemas>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizeEventTypeSchemasValidator"/> class.
    /// </summary>
    public NormalizeEventTypeSchemasValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
    }
}
