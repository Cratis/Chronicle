// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="CompleteStream"/>.
/// </summary>
internal class CompleteStreamValidator : CommandValidator<CompleteStream>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStreamValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public CompleteStreamValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.EventSequenceId).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).NotReservedForNewEventSequence(storage, reservedEventSequenceIds, _ => _.EventStore, _ => _.Namespace);

        // The stream identifiers are not required here: the empty value is the default stream, and
        // completing the default stream is a business outcome the handler reports as
        // `DefaultStreamCannotBeCompleted` - not an input error to reject before it is asked.
    }
}
