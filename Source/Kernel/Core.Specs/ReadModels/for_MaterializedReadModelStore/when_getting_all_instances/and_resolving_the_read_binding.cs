// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_getting_all_instances;

public class and_resolving_the_read_binding : given.a_materialized_read_model
{
    void Establish() => SinkHolds(InstanceNamed("first"), InstanceNamed("second"));

    async Task Because() => await _store.GetAllInstances(EventStore, EventStoreNamespace, _definition);

    [Fact] void should_resolve_once_for_the_read() => _readBindings.Received(1).Resolve(EventStore, EventStoreNamespace, _definition);
    [Fact] void should_read_the_bound_sink() => _sink.Received(1).GetInstances(null, 0, 100);
}
