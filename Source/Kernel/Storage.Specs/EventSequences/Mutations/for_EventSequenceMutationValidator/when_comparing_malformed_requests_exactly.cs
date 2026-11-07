// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator;

public class when_comparing_malformed_requests_exactly : given.a_mutation_validation
{
    bool[] _requestMatches;
    bool[] _definitionMatches;
    bool[] _registrationMatches;

    void Because()
    {
        var malformed = new[]
        {
            _request with { TargetSequence = null! },
            _request with { Origin = null! },
            _request with { Origin = _request.Origin with { Sequence = null! } },
            _request with { Origin = _request.Origin with { SequenceNumber = null! } },
            _request with { Command = null! },
            _request with { Command = _request.Command with { Payload = null! } },
            _request with { Command = _request.Command with { Hash = null! } }
        };
        var registration = new EventSequenceMutationRegistration(
            _definition,
            EventSequenceMutationRegistryLifecycle.Claimed,
            null,
            null);
        _requestMatches = malformed.Select(_request.ExactlyEquals).ToArray();
        _definitionMatches = malformed.Select(_definition.IsExactRequest).ToArray();
        _registrationMatches = malformed.Select(registration.IsExactRequest).ToArray();
    }

    [Fact] void should_not_match_malformed_requests() => _requestMatches.All(_ => !_).ShouldBeTrue();
    [Fact] void should_not_throw_or_match_through_a_definition() => _definitionMatches.All(_ => !_).ShouldBeTrue();
    [Fact] void should_not_throw_or_match_through_a_registration() => _registrationMatches.All(_ => !_).ShouldBeTrue();
}
