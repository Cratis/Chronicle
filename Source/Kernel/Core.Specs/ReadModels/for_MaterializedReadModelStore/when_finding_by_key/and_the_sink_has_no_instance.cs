// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_key;

public class and_the_sink_has_no_instance : given.a_materialized_read_model
{
    static readonly Key _theKey = new("read-model-key", ArrayIndexers.NoIndexers);

    ExpandoObject _result;

    void Establish() => _sink.FindInPrimaryOrDefault(_theKey).Returns((ExpandoObject?)null);

    async Task Because() => _result = await _store.FindByKey(EventStore, EventStoreNamespace, _definition, _theKey);

    [Fact] void should_read_the_primary() => _sink.Received(1).FindInPrimaryOrDefault(_theKey);
    [Fact] void should_not_read_projection_state() => _sink.DidNotReceive().FindOrDefault(Arg.Any<Key>());
    [Fact] void should_return_nothing() => _result.ShouldBeNull();
    [Fact] void should_not_release_anything() => _compliance.DidNotReceive().Release(
        Arg.Any<EventStoreName>(),
        Arg.Any<EventStoreNamespaceName>(),
        Arg.Any<JsonSchema>(),
        Arg.Any<ExpandoObject>());
}
