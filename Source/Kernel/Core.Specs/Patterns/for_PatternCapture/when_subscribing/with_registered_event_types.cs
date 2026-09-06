// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reactors;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// What pattern capture mines is the context an event was appended in, not anything about a particular event type,
/// so it has to observe all of them. An observer subscribes to a list, so "everything" is the list of everything
/// currently registered.
/// </summary>
public class with_registered_event_types : given.a_pattern_capture
{
    ReactorDefinition _definition;

    async Task Because()
    {
        EventTypesAre("ExpenseReportSubmitted", "ExpenseReportApproved", "ExpenseReportRejected");
        await _capture.Subscribe(_eventStore, _namespace);
        _definition = _reactors.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IReactorDefinitionsStorage.Save))
            .Select(call => (ReactorDefinition)call.GetArguments()[0]!)
            .Single();
    }

    [Fact] async Task should_subscribe_the_observer_to_every_registered_event_type() =>
        await _observer.Received(1).Subscribe<IPatternCaptureSubscriber>(
            ObserverType.Reactor,
            Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.Count() == 3),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            false,
            Arg.Any<ObserverFilters?>());

    [Fact] void should_register_it_as_owned_by_the_kernel() => _definition.Owner.ShouldEqual(ReactorOwner.Kernel);
    [Fact] void should_register_it_under_the_pattern_capture_identifier() => _definition.Identifier.Value.ShouldEqual(PatternCapture.ObserverIdentifier);
    [Fact] void should_register_every_event_type() => _definition.EventTypes.Count().ShouldEqual(3);
    [Fact] void should_not_be_replayable() => _definition.IsReplayable.ShouldBeFalse();
}
