// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_no_keys_match_the_reader_scope : given.a_catch_up_observer_job
{
    static readonly ObserverReaderScope _scope = ObserverReaderScope.For(false, [new EventType("empty-scope-event", 2U)]);

    void Establish() => _request = _request with { ObserverType = ObserverType.Reactor, FromEventSequenceNumber = 42L, ReaderScope = _scope };

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_no_steps() => _job.PreparedSteps.ShouldBeEmpty();
    [Fact] async Task should_use_the_explicit_scope() => await _keyIndexes.Received(1).GetFor(_request.ObserverKey, _scope);
    [Fact] void should_enumerate_from_the_requested_position() => _keyIndex.Received(1).GetKeys(_request.FromEventSequenceNumber);
}
