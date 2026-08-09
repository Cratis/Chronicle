// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_key;

public class and_the_sink_holds_the_instance : given.a_materialized_read_model
{
    static readonly Key _theKey = new("read-model-key", ArrayIndexers.NoIndexers);

    ExpandoObject _released;
    ExpandoObject _result;

    void Establish()
    {
        _sink.FindOrDefault(_theKey).Returns(InstanceNamed("encrypted"));

        _released = InstanceNamed("released");
        _compliance.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<ExpandoObject>())
            .Returns(_released);
    }

    async Task Because() => _result = await _store.FindByKey(EventStore, EventStoreNamespace, _definition, _theKey);

    [Fact] void should_return_the_released_instance() => _result.ShouldEqual(_released);
    [Fact] void should_release_what_the_sink_held() => _compliance.Received(1).Release(
        EventStore,
        EventStoreNamespace,
        Arg.Any<JsonSchema>(),
        Arg.Any<ExpandoObject>());
}
