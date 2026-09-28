// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="Append"/>.
/// </summary>
internal class AppendValidator : CommandValidator<Append>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendValidator"/> class.
    /// </summary>
    public AppendValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSourceId).RequiredEventSource();

        // Omitted or empty routing metadata is accepted for older clients and direct API callers.
        // The handler resolves it to canonical append defaults before validating constraints or persisting.
        RuleFor(_ => _.EventType).RequiredEventType();
        RuleFor(_ => _.EventType).RequiredEventTypeIdentifier();
        RuleFor(_ => _.Content).RequiredContent();
    }
}
