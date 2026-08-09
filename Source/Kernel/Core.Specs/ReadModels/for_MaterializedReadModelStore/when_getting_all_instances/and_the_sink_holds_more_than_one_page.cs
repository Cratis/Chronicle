// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_getting_all_instances;

public class and_the_sink_holds_more_than_one_page : given.a_materialized_read_model
{
    const int InstanceCount = 250;

    ExpandoObject[] _stored;
    IEnumerable<ExpandoObject> _result;

    void Establish()
    {
        _stored = [.. Enumerable.Range(0, InstanceCount).Select(index => InstanceNamed($"instance-{index}"))];
        SinkHolds(_stored);
    }

    async Task Because() => _result = await _store.GetAllInstances(EventStore, EventStoreNamespace, _definition);

    [Fact] void should_page_through_every_instance() => _result.Count().ShouldEqual(InstanceCount);
    [Fact] void should_keep_the_order_the_sink_returned_them_in() => _result.ShouldEqual(_stored);
    [Fact] void should_release_every_page() => _compliance.Received(3).Release(
        EventStore,
        EventStoreNamespace,
        Arg.Any<JsonSchema>(),
        Arg.Any<IEnumerable<ExpandoObject>>());

    [Fact] void should_stop_reading_when_the_total_is_reached() => _sink.Received(3).GetInstances(
        Arg.Any<ReadModelContainerName?>(),
        Arg.Any<int>(),
        Arg.Any<int>());
}
