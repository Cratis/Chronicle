// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_the_sink_holds_the_instance : given.a_composite_key_store
{
    ExpandoObject _released;
    void Establish()
    {
        _released = InstanceNamed("released");
        _lookup.FindByCompositeKey(_key).Returns(CompositeKeyLookupResult.Found(InstanceNamed("stored")));
        _compliance.Release(EventStore, EventStoreNamespace, _definition.GetSchemaForLatestGeneration(), Arg.Any<ExpandoObject>()).Returns(_released);
    }
    Task Because() => Find();
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_return_only_the_released_instance() => _result.Instance.ShouldEqual(_released);
    [Fact] Task should_use_the_structured_sink_lookup() => _lookup.Received(1).FindByCompositeKey(_key);
}
