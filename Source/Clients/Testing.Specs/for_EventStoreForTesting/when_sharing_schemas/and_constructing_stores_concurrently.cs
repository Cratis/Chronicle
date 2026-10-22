// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_constructing_stores_concurrently : given.schema_sharing
{
    EventStoreForTesting[] _stores;
    string[] _json;

    async Task Because()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return new EventStoreForTesting(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(ConcurrentEvent)] });
        })).ToArray();
        start.SetResult();
        _stores = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _json = _stores.Select(store => store.EventTypes.GetSchemaFor(store.EventTypes.GetEventTypeFor(typeof(ConcurrentEvent)).Id).ToJson()).ToArray();
    }

    [Fact] void should_construct_all_stores() => _stores.Length.ShouldEqual(8);
    [Fact] void should_share_the_underlying_generator() => _stores.All(store => ReferenceEquals(GeneratorFor(store), GeneratorFor(_store))).ShouldBeTrue();
    [Fact] void should_generate_identical_schemas() => _json.Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_each_schema_separate() => _stores.Select(store => store.EventTypes.GetSchemaFor(store.EventTypes.GetEventTypeFor(typeof(ConcurrentEvent)).Id)).Distinct().Count().ShouldEqual(8);

    void Destroy()
    {
        foreach (var store in _stores ?? [])
        {
            store.Dispose();
        }
    }

    [EventType]
    public record ConcurrentEvent(string Value, int Count);
}
