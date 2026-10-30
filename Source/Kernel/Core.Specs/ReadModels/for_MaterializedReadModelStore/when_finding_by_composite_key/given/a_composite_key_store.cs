// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key.given;

public class a_composite_key_store : for_MaterializedReadModelStore.given.a_materialized_read_model
{
    protected ISinkCompositeKeyLookup _lookup;
    protected Key _key;
    protected CompositeKeyLookupResult _result;
    void Establish()
    {
        _sink = Substitute.For<ISink, ISinkCompositeKeyLookup>();
        _lookup = (ISinkCompositeKeyLookup)_sink;
        _readBindings.Resolve(EventStore, EventStoreNamespace, _definition).Returns(new ReadModelReadBinding(_sink, "occurrence"));
        _key = new Key(new ExpandoObject(), ArrayIndexers.NoIndexers);
    }
    protected async Task Find() => _result = await _store.FindByCompositeKey(EventStore, EventStoreNamespace, _definition, _key);
    protected Task DidNotRelease() => _compliance.DidNotReceive().Release(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<ExpandoObject>());
}
