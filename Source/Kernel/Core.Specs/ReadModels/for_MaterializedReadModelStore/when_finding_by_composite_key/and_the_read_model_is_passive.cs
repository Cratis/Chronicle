// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_the_read_model_is_passive : given.a_composite_key_store
{
    void Establish() => _definition = _definition with { Sink = SinkDefinition.None };
    Task Because() => Find();
    [Fact] void should_report_not_supported() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotSupported);
    [Fact] Task should_not_resolve_a_read_binding() => _readBindings.DidNotReceive().Resolve(EventStore, EventStoreNamespace, _definition);
    [Fact] Task should_not_release_compliance() => DidNotRelease();
}
