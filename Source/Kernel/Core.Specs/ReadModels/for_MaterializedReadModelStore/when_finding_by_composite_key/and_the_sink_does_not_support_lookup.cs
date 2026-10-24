// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_the_sink_does_not_support_lookup : given.a_composite_key_store
{
    void Establish() => _readBindings.Resolve(EventStore, EventStoreNamespace, _definition).Returns(new ReadModelReadBinding(Substitute.For<ISink>(), null));
    Task Because() => Find();
    [Fact] void should_report_not_supported() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotSupported);
    [Fact] Task should_not_release_compliance() => DidNotRelease();
}
