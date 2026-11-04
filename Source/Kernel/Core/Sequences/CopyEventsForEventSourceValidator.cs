// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Validates <see cref="CopyEventsForEventSource"/>.
/// </summary>
internal class CopyEventsForEventSourceValidator : CommandValidator<CopyEventsForEventSource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopyEventsForEventSourceValidator"/> class.
    /// </summary>
    public CopyEventsForEventSourceValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.EventSequenceId).NoKeySeparator();
        RuleFor(_ => _.SourceId).NotEmpty().WithMessage("A source event source id is required.");
        RuleFor(_ => _.TargetId).NotEmpty().WithMessage("A target event source id is required.");
        RuleFor(_ => _).Must(_ => _.SourceId != _.TargetId).WithMessage("The source and target event source must differ.");
    }
}
