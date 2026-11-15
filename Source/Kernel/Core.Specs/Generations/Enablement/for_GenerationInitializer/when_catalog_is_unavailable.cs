// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_catalog_is_unavailable : given.an_initializer
{
    async Task Because() => _result = await Create(false).Initialize(_name);
    [Fact] async Task should_record_unavailable() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.CatalogUnavailable);
    [Fact] void should_not_enable() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] async Task should_record_no_receipts() => (await _audit.GetReceipts()).ShouldBeEmpty();
    [Fact] async Task should_leave_the_store_not_started() => (await _audit.GetProgress()).ShouldEqual(GenerationInitializationProgress.NotStarted);
    [Fact] void should_not_check_proof() => _proof.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_probe_membership() => _membership.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_legacy_work() => _legacy.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_materializations() => _models.ReceivedCalls().ShouldBeEmpty();
}
