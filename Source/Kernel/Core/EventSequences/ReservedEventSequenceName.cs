// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when a new event sequence uses a reserved storage name.
/// </summary>
/// <param name="eventSequenceId">The refused event sequence identifier.</param>
public class ReservedEventSequenceName(EventSequenceId eventSequenceId)
    : Exception(ReservedEventSequenceRules.Message(eventSequenceId.Value)), IValidationFailure
{
    static readonly string[] _members = ["eventSequenceId"];

    /// <inheritdoc/>
    public ValidationResult ValidationResult => new(ValidationResultSeverity.Error, Message, _members, null!);
}
