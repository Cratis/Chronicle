// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_handling;

/// <summary>
/// A replay is a direct call on the observer grain, which also serves client subscriptions: while the grain is busy the
/// call is queued behind them and the command does not complete (it times out after 30 seconds in a real silo).
/// </summary>
public class and_the_observer_is_busy : Specification
{
    readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    readonly TaskCompletionSource<JobId> _grainCall = new();
    IStorage _storage;
    IGrainFactory _grainFactory;
    Task<ValidationResult?> _handling;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(definitions);
        definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        var observer = Substitute.For<IObserver>();
        observer.Replay().Returns(_grainCall.Task);
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because()
    {
        _handling = new ReplayObserver("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage);
        await Task.Delay(200);
    }

    void Destroy() => _grainCall.TrySetResult(JobId.New());

    [Fact] void should_still_be_waiting_for_the_observer() => _handling.IsCompleted.ShouldBeFalse();
}
