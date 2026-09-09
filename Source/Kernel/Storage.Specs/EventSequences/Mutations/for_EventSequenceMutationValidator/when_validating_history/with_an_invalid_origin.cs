// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_an_invalid_origin : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = null! }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { Sequence = null! } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { Sequence = IdentityWithKey("system", default) } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { Sequence = IdentityWithKeyFrom("system", "other") } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { SequenceNumber = null! } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { SequenceNumber = EventSequenceNumber.Unavailable } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { SequenceNumber = EventSequenceNumber.Max } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Origin = _history.Origin with { SequenceNumber = EventSequenceNumber.BeforeFirst } })
    ];

    [Fact] void should_reject_every_missing_mismatched_and_non_actual_origin() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidIdentity).ShouldBeTrue();
}
