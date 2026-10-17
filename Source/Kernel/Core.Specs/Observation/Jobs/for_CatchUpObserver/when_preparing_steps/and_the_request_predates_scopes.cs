// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// A request persisted before requests carried a scope has no scope and no event types for an observer
/// subscribed to all events. It is read as a wildcard, as an empty type list always was.
/// </summary>
public class and_the_request_predates_scopes : given.a_catch_up_observer_job
{
    static readonly Key _key = (Key)"partition-1";

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_look_the_key_index_up_with_a_wildcard() => _keyIndexes.Received(1).GetFor(_request.ObserverKey, Arg.Is<ObserverReaderScope>(_ => _.IsWildcard));
}
