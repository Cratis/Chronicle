// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_getting_all_instances;

public class and_the_projection_is_child_only : given.a_materialized_read_model
{
    ExpandoObject _stored;
    IEnumerable<ExpandoObject> _result;

    void Establish()
    {
        _stored = InstanceNamed("Child-only before upgrade");
        _readBindings.Resolve(EventStore, EventStoreNamespace, _definition)
            .Returns(new ReadModelReadBinding(_sink, null, ReadModelInstanceVisibility.IncludeUninitialized));
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>(), ReadModelInstanceVisibility.IncludeUninitialized)
            .Returns(new ReadModelInstances([_stored], 1));
    }

    async Task Because() => _result = await _store.GetAllInstances(EventStore, EventStoreNamespace, _definition);

    [Fact] void should_return_the_root() => _result.ShouldContainOnly([_stored]);
    [Fact] void should_read_with_the_uninitialized_visibility() => _sink.Received(1).GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Is(0), Arg.Any<int>(), ReadModelInstanceVisibility.IncludeUninitialized);
}
