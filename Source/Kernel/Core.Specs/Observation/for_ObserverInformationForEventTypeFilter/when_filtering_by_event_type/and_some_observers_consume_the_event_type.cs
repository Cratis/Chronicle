// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverInformationForEventTypeFilter.when_filtering_by_event_type;

public class and_some_observers_consume_the_event_type : Specification
{
    const string Namespace = "some-namespace";
    const string MatchingEventTypeId = "matching-event-type";

    ObserverInformation _matchingObserver;
    ObserverInformation _nonMatchingObserver;
    IEnumerable<ObserverInformationForEventType> _result;

    void Establish()
    {
        _matchingObserver = ObserverConsuming("matching-observer", MatchingEventTypeId);
        _nonMatchingObserver = ObserverConsuming("non-matching-observer", "other-event-type");
    }

    void Because() => _result = ObserverInformationForEventTypeFilter.FilterByEventType(
        Namespace,
        [_matchingObserver, _nonMatchingObserver],
        MatchingEventTypeId);

    [Fact] void should_only_return_the_matching_observer() => _result.Single().Observer.Id.ShouldEqual("matching-observer");
    [Fact] void should_carry_the_namespace() => _result.Single().Namespace.ShouldEqual(Namespace);

    static ObserverInformation ObserverConsuming(string observerId, string eventTypeId) =>
        new(
            observerId,
            "event-log",
            ObserverType.Reactor,
            ObserverOwner.Client,
            [new Contracts.Events.EventType { Id = eventTypeId, Generation = 1 }],
            0,
            0,
            0,
            0,
            ObserverRunningState.Active,
            IsSubscribed: false,
            IsReplayable: true);
}
