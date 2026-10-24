// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_all_materializations_are_adopted : given.an_initializer
{
    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] void should_enable() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] void should_keep_provenance_unknown() => _catalog.Records.Values.All(_ => _.Revision == MaterializationRevision.Unknown).ShouldBeTrue();
    [Fact] void should_adopt_generation_one() => _catalog.Records.Values.All(_ => _.Identity.Generation == MaterializationGeneration.First).ShouldBeTrue();
    [Fact] void should_keep_physical_containers() => _catalog.Records.Values.Select(_ => _.Identity.Destination.Value).Order().ShouldEqual(["items", "orders"]);
    [Fact] void should_set_epoch_one() => _catalog.Epochs.Values.All(_ => _ == 1).ShouldBeTrue();
    [Fact] void should_adopt_every_subject_before_binding() => _catalog.BoundBeforeAllAdopted.ShouldBeFalse();
    [Fact] async Task should_write_enabled_last() => (await _audit.GetReceipts())[^1].Step.ShouldEqual(GenerationInitializationStep.Enabled);
    [Fact] void should_not_access_namespace_mutators() => _store.DidNotReceiveWithAnyArgs().GetNamespace(Arg.Any<EventStoreNamespaceName>());
}
