// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut.given;

public class a_fan_out : Specification
{
    protected IEventStoreStorage _store;
    protected Storage.InMemory.Occurrences.LifecycleFanOutStorage _intents;
    protected readonly Storage.InMemory.Occurrences.OccurrencesStorage _application = new();
    protected readonly Dictionary<string, Storage.InMemory.Occurrences.OccurrencesStorage> _namespaces = [];
    protected readonly List<NamespaceState> _snapshot = [];
    protected LifecycleFanOut _fanOut;
    protected bool _interrupt;
    protected int _writes;

    async Task Establish()
    {
        _store = Substitute.For<IEventStoreStorage>();
        _intents = new();
        _store.LifecycleFanOut.Returns(_intents);
        _store.Occurrences.Returns(_application);
        var definitions = new Storage.InMemory.Occurrences.TriggerDefinitionsStorage();
        await definitions.TrySave(new(new("startup"), "Startup", "{}", 1), null);
        await definitions.TrySave(new(new("shutdown"), "Shutdown", "{}", 1), null);
        _store.TriggerDefinitions.Returns(definitions);
        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns(_ => _snapshot.ToArray());
        _store.Namespaces.Returns(namespaces);
        AddNamespace("one");
        AddNamespace("two");
        _fanOut = Recreate();
    }

    protected LifecycleFanOut Recreate() => new(_store, BeforeWrite, TimeProvider.System, NullLogger<LifecycleFanOut>.Instance);

    Task BeforeWrite(string? scope, string trigger)
    {
        if (scope is not null && ++_writes == 2 && _interrupt)
        {
            throw new IOException("simulated crash between namespace writes");
        }

        return Task.CompletedTask;
    }

    protected void AddNamespace(string name)
    {
        _snapshot.Add(new(new EventStoreNamespaceName(name), DateTimeOffset.UtcNow));
        var storage = new Storage.InMemory.Occurrences.OccurrencesStorage();
        _namespaces[name] = storage;
        var scoped = Substitute.For<IEventStoreNamespaceStorage>();
        scoped.Occurrences.Returns(storage);
        _store.GetNamespace(new EventStoreNamespaceName(name)).Returns(scoped);
    }
}
