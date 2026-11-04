// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventStores.for_LegacyNameReconnect;

public class when_a_registered_legacy_store_reconnects : given.a_legacy_installation
{
    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    CommandResult _validation;
    IEventStoreStorage _eventStore;
    IEventStoreNamespaceStorage _namespace;
    Exception _error;

    void Establish() => _scenario.Services.AddSingleton(_storage);

    async Task Because()
    {
        _validation = await _scenario.Validate(new EnsureEventStore(_legacyStore.Value));
        _error = await Catch.Exception(async () =>
        {
            _eventStore = _storage.GetEventStore(_legacyStore);
            _namespace = _storage.GetEventStore(_plainStore).GetNamespace(_legacyNamespace);
            await _namespace.GetEventSequence(Concepts.EventSequences.EventSequenceId.Log).GetState();
        });
    }

    [Fact] void should_accept_the_name_when_ensuring() => _validation.ShouldBeValid();
    [Fact] void should_open_the_event_store() => _eventStore.EventStore.ShouldEqual(_legacyStore);
    [Fact] void should_open_the_legacy_namespace() => _namespace.ShouldNotBeNull();
    [Fact] void should_reach_the_event_sequence() => _error.ShouldBeNull();
}
