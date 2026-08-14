// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation.for_ObserverInformationForEventTypeFilter.when_filtering_by_event_type;

public class and_some_observers_consume_the_event_type : Specification
{
    const string Namespace = "some-namespace";
    const string MatchingEventTypeId = "matching-event-type";

    Contracts.Observation.ObserverInformation _matchingObserver;
    Contracts.Observation.ObserverInformation _nonMatchingObserver;
    IEnumerable<ObserverInformationForEventType> _result;

    void Establish()
    {
        _matchingObserver = new Contracts.Observation.ObserverInformation
        {
            Id = "matching-observer",
            EventSequenceId = "event-log",
            EventTypes = [new Contracts.Events.EventType { Id = MatchingEventTypeId, Generation = 1 }]
        };
        _nonMatchingObserver = new Contracts.Observation.ObserverInformation
        {
            Id = "non-matching-observer",
            EventSequenceId = "event-log",
            EventTypes = [new Contracts.Events.EventType { Id = "other-event-type", Generation = 1 }]
        };
    }

    void Because() => _result = ObserverInformationForEventTypeFilter.FilterByEventType(
        Namespace,
        [_matchingObserver, _nonMatchingObserver],
        MatchingEventTypeId);

    [Fact] void should_only_return_the_matching_observer() => _result.Single().Observer.Id.ShouldEqual("matching-observer");
    [Fact] void should_carry_the_namespace() => _result.Single().Namespace.ShouldEqual(Namespace);
}
