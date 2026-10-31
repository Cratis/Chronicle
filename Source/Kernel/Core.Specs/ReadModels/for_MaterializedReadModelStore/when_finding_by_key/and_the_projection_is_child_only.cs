// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_key;

public class and_the_projection_is_child_only : given.a_materialized_read_model
{
    ExpandoObject _stored;
    ExpandoObject? _result;

    void Establish()
    {
        _stored = InstanceNamed("Child-only before upgrade");
        ((IDictionary<string, object?>)_stored)[WellKnownProperties.ReadModelInstanceInitialized] = false;
        _readBindings.Resolve(EventStore, EventStoreNamespace, _definition)
            .Returns(new ReadModelReadBinding(_sink, null, ReadModelInstanceVisibility.IncludeUninitialized));
        _sink.FindInPrimaryOrDefault("the-key").Returns(_stored);
    }

    async Task Because() => _result = await _store.FindByKey(EventStore, EventStoreNamespace, _definition, "the-key");

    [Fact] void should_return_the_root() => _result.ShouldEqual(_stored);
}
