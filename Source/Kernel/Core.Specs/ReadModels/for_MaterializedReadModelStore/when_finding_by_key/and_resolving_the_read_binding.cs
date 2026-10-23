// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_key;

public class and_resolving_the_read_binding : given.a_materialized_read_model
{
    static readonly Key _key = new("key", ArrayIndexers.NoIndexers);

    async Task Because() => await _store.FindByKey(EventStore, EventStoreNamespace, _definition, _key);

    [Fact] void should_resolve_once() => _readBindings.Received(1).Resolve(EventStore, EventStoreNamespace, _definition);
    [Fact] void should_read_the_bound_sink() => _sink.Received(1).FindInPrimaryOrDefault(_key);
}
