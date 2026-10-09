// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.given;

public class registrations : Specification
{
    protected Reactors _reactors;
    protected IObserver _observer;
    protected IReactorDefinitionsStorage _definitions;
    protected IReactorDefinitionsStorage _otherDefinitions;
    protected ConcurrentDictionary<ReactorId, ReactorDefinition> _persisted = new();
    protected ConcurrentDictionary<ReactorId, ReactorDefinition> _otherPersisted = new();
    protected static readonly EventStoreName _eventStore = "first";
    protected static readonly EventStoreName _otherEventStore = "second";

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IReactor>().Returns([typeof(TenantReactor)]);
        var storage = Substitute.For<IStorage>();
        var first = Substitute.For<IEventStoreStorage>();
        var second = Substitute.For<IEventStoreStorage>();
        _definitions = Definitions(_persisted);
        _otherDefinitions = Definitions(_otherPersisted);
        first.Reactors.Returns(_definitions);
        second.Reactors.Returns(_otherDefinitions);
        storage.GetEventStore(_eventStore).Returns(first);
        storage.GetEventStore(_otherEventStore).Returns(second);
        var grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(_observer);
        _reactors = new(types, Substitute.For<ILocalSiloDetails>(), storage, grainFactory);
    }

    async Task Destroy() => await _reactors.DisposeAsync();

    static IReactorDefinitionsStorage Definitions(ConcurrentDictionary<ReactorId, ReactorDefinition> persisted)
    {
        var definitions = Substitute.For<IReactorDefinitionsStorage>();
        definitions.Has(Arg.Any<ReactorId>()).Returns(call => persisted.ContainsKey(call.Arg<ReactorId>()));
        definitions.Get(Arg.Any<ReactorId>()).Returns(call => persisted[call.Arg<ReactorId>()]);
        definitions.Save(Arg.Any<ReactorDefinition>()).Returns(call =>
        {
            var definition = call.Arg<ReactorDefinition>();
            persisted[definition.Identifier] = definition;
            return Task.CompletedTask;
        });
        return definitions;
    }

    [Reactor(systemEventStoreOnly: false, defaultNamespaceOnly: false)]
    public class TenantReactor : Reactor
    {
        public Task NamespaceAdded(NamespaceAdded @event, EventContext context) => Task.CompletedTask;
    }
}
