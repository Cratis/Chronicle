// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

public class and_the_observer_merges_newer_registrations : given.a_pattern_capture
{
    EventType[] _effectiveTypes;

    void Establish()
    {
        EventTypesAre("CustomerNamed");
        _effectiveTypes = [new EventType("CustomerNamed", 2), new EventType("CustomerMoved", EventTypeGeneration.First)];
        _observer.SubscribeAdditively<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false)
            .Returns(_effectiveTypes);
    }

    Task Because() => _capture.Subscribe(_eventStore, _namespace);

    [Fact] async Task should_save_the_effective_types_instead_of_the_older_registry_snapshot() => await _reactors.Received(1).Save(Arg.Is<ReactorDefinition>(definition => definition.EventTypes.Select(type => type.EventType).ToHashSet().SetEquals(_effectiveTypes)));
    [Fact] void should_save_only_after_the_observer_has_merged_the_subscription() => Received.InOrder(() =>
    {
        _observer.SubscribeAdditively<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false);
        _reactors.Save(Arg.Any<ReactorDefinition>());
    });
}
