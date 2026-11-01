// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for scoped stream completion.
/// </summary>
internal class CompleteStreamScopeValidator : CommandValidator<CompleteStreamScope>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStreamScopeValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public CompleteStreamScopeValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
    {
        RuleFor(command => command.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(command => command.EventStore).NotSystemEventStore();
        RuleFor(command => command.EventStore).NoKeySeparator();
        RuleFor(command => command.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(command => command.Namespace).NoKeySeparator();
        RuleFor(command => command.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(command => command.EventSequenceId).NoKeySeparator();
        RuleFor(command => command.EventSequenceId).NotReservedForNewEventSequence(storage, reservedEventSequenceIds, command => command.EventStore, command => command.Namespace);
    }
}
