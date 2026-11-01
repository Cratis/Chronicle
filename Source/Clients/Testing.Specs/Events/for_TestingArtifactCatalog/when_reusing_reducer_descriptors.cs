// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_reusing_reducer_descriptors : Specification
{
    EventStoreForTesting[] _stores;

    async Task Because() => _stores = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => new EventStoreForTesting(null,
        new InMemoryClientArtifactsProvider { EventTypes = [typeof(Recorded)], Reducers = [typeof(Model)] }))));

    [Fact] void should_build_once_even_for_concurrent_stores() => _stores[0].Metadata!.ReducerDescriptors.BuildCount.ShouldEqual(1);
    [Fact] void should_share_the_descriptor_source() => _stores.All(store => ReferenceEquals(store.Metadata, _stores[0].Metadata)).ShouldBeTrue();
    [Fact] void should_register_each_store_locally() => _stores.All(store => store.Reducers.GetAllHandlers().Any()).ShouldBeTrue();

    void Destroy()
    {
        foreach (var store in _stores ?? []) store.Dispose();
    }

    [EventType("catalog-reducer-recorded")] public record Recorded(string Value);
    public record Model(string Value)
    {
        public static Model Reduce(Recorded @event, Model? current) => new(@event.Value);
    }
}
