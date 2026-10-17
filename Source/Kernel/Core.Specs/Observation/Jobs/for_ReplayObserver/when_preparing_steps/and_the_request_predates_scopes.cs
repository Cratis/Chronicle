// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_the_request_predates_scopes : given.a_replay_observer_job
{
    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys((Key)"partition-1"));
    }

    async Task Because() => await _job.PrepareStepsForTesting(_request);

    [Fact] void should_look_the_key_index_up_with_a_wildcard() => _keyIndexes.Received(1).GetFor(_request.ObserverKey, Arg.Is<ObserverReaderScope>(_ => _.IsWildcard));
}
