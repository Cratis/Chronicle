// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_invalid_origin_position : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRequest(_request with { Origin = _request.Origin with { SequenceNumber = null! } }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Origin = _request.Origin with { SequenceNumber = EventSequenceNumber.Unavailable } }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Origin = _request.Origin with { SequenceNumber = EventSequenceNumber.Max } }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Origin = _request.Origin with { SequenceNumber = EventSequenceNumber.BeforeFirst } })
    ];

    [Fact] void should_reject_every_non_actual_position() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidIdentity).ShouldBeTrue();
}
