// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.given;

public class a_replay_command : Specification
{
    protected readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    protected readonly CommandScenario<ReplayObserver> _scenario = ChronicleCommandScenario.For<ReplayObserver>();
    protected IGrainFactory _grainFactory;
    protected IObserver _observer;
    protected CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(definitions);
        definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        _grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _observer.Replay().Returns(JobId.New());
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _scenario.Services.AddSingleton(storage);
        _scenario.Services.AddSingleton(_grainFactory);
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
