// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// A namespace where capture is off costs nothing: no observer is resolved, nothing is subscribed and no definition written.
/// </summary>
public class and_capture_is_off_for_the_event_store : given.a_pattern_capture
{
    async Task Establish()
    {
        _options.PatternDetection.Overrides[_eventStore.Value] = false;
        EventTypesAre("some-event");
        await Task.CompletedTask;
    }

    async Task Because()
    {
        await _capture.Subscribe(_eventStore, _namespace);
        await _capture.RecoverSubscription(_eventStore, _namespace);
    }

    [Fact] void should_not_resolve_an_observer() => _grainFactory.DidNotReceiveWithAnyArgs().GetGrain<IObserver>(default!, default);
    [Fact] async Task should_not_subscribe_an_observer() =>
        await _observer.DidNotReceiveWithAnyArgs().SubscribeAdditively<IPatternCaptureSubscriber>(default, default!, default!, default, default, default);
    [Fact] async Task should_not_save_a_definition() => await _reactors.DidNotReceiveWithAnyArgs().Save(default!);
}
