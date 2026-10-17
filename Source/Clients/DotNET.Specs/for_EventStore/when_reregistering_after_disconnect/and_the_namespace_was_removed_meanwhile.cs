// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_namespace_was_removed_meanwhile : given.a_registered_store
{
    void Establish()
    {
        _store.DetachAutomaticRegistration();
        _namespace = "tenant";
        _store = CreateStore();
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>())
            .Returns(Task.FromResult(QueryResult<IEnumerable<NamespaceNamesResponse>>.Success(Guid.NewGuid(), [new() { Name = "default" }])));
        _services.EventStores.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _connection.Lifecycle.Connected();
        _services.EventStores.ClearReceivedCalls();
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_retire_the_event_store() => _store.IsRetired.ShouldBeTrue();
    [Fact] void should_not_register_again_and_so_not_bring_the_namespace_back() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
}
