// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// A namespace entry overrides the event store, so one tenant can be off while the rest of its store captures.
/// </summary>
public class and_capture_is_off_for_another_namespace : given.a_pattern_capture
{
    async Task Establish()
    {
        _options.PatternDetection.Overrides[$"{_eventStore.Value}/other-tenant"] = false;
        EventTypesAre("some-event");
        await Task.CompletedTask;
    }

    async Task Because()
    {
        await _capture.Subscribe(_eventStore, "other-tenant");
        await _capture.Subscribe(_eventStore, _namespace);
    }

    [Fact] async Task should_subscribe_only_the_namespace_that_is_on() =>
        await _observer.Received(1).SubscribeAdditively<IPatternCaptureSubscriber>(
            Arg.Any<ObserverType>(),
            Arg.Any<EventTypeSet>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>());
}
