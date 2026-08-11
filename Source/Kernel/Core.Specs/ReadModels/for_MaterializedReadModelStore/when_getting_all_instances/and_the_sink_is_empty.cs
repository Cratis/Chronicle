// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_getting_all_instances;

public class and_the_sink_is_empty : given.a_materialized_read_model
{
    IEnumerable<ExpandoObject> _result;

    void Establish() => SinkHolds();

    async Task Because() => _result = await _store.GetAllInstances(EventStore, EventStoreNamespace, _definition);

    [Fact] void should_return_no_instances() => _result.ShouldBeEmpty();
    [Fact] void should_not_release_anything() => _compliance.DidNotReceive().Release(
        Arg.Any<EventStoreName>(),
        Arg.Any<EventStoreNamespaceName>(),
        Arg.Any<JsonSchema>(),
        Arg.Any<IEnumerable<ExpandoObject>>());
}
