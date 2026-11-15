// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Namespaces.for_NamespacesStateStorageProvider.when_reading;

public class and_two_casings_are_stored : Specification
{
    IEventStoreStorage _eventStore;
    NamespacesStateStorageProvider _provider;
    GrainState<NamespacesState> _state;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        _eventStore = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore("Studio").Returns(_eventStore);
        _eventStore.Namespaces.GetAll().Returns([new("cratis", DateTimeOffset.MinValue), new("Cratis", DateTimeOffset.MaxValue)]);
        _eventStore.HasOwnNamespaceData("Cratis").Returns(true);
        _provider = new(storage, NullLogger<NamespacesStateStorageProvider>.Instance);
        _state = new();
    }

    async Task Because() => await _provider.ReadStateAsync("namespaces", GrainId.Create("namespaces", "Studio"), _state);

    [Fact] void should_keep_only_the_name_with_data_and_default() => _state.State.Namespaces.Select(_ => _.Name.Value).ShouldEqual(["Cratis", "Default"]);
    [Fact] async Task should_delete_only_the_variant_list_entry() => await _eventStore.Namespaces.Received(1).Delete("cratis");
    [Fact] async Task should_not_drop_any_namespace() => await _eventStore.DidNotReceiveWithAnyArgs().DropNamespace(default!);
}
