// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_comparing_requests_exactly;

public class and_every_field_is_equal : given.a_mutation_validation
{
    EventSequenceMutationRequest _candidate;
    bool _result;

    void Establish() => _candidate = new(
        _request.Id,
        Identity(_request.TargetSequence.Display),
        new(Identity(_request.Origin.Sequence.Display), _request.Origin.SequenceNumber),
        _request.Kind,
        new(_request.Command.Payload, _request.Command.Hash.Value));

    void Because() => _result = _request.ExactlyEquals(_candidate);

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
}
