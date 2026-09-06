// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using context = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_the_same_scope_in_two_namespaces.context;
using EventFactory = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_behavior_for_a_scope.context;

namespace Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner;

/// <summary>
/// A namespace is a tenant, and the miner's grain key is what isolates one tenant's behavior from another's. The
/// same scope name mined in two namespaces reaches two different grains, and each namespace's storage holds only
/// what its own tenant did.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_mining_the_same_scope_in_two_namespaces(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public const string Scope = "ingrid.holm";

        public bool BothTenantsEstablishedBehavior;
        public bool OtherTenantBehaviorLeakedIntoTheFirst;
        public bool FirstTenantBehaviorLeakedIntoTheOther;
        public bool OnlyTheTenantOwnObservationsCounted;

        readonly ConceptsEventStoreName _eventStore = $"patterns-{Guid.NewGuid():N}";
        readonly ConceptsEventStoreNamespaceName _firstTenant = "first-tenant";
        readonly ConceptsEventStoreNamespaceName _otherTenant = "other-tenant";

        async Task Because()
        {
            var extractor = Services.GetRequiredService<IEventFeatureExtractor>();
            var grainFactory = Services.GetRequiredService<IGrainFactory>();
            var firstMiner = grainFactory.GetGrain<IPatternMiner>(new PatternMinerKey(_eventStore, _firstTenant));
            var otherMiner = grainFactory.GetGrain<IPatternMiner>(new PatternMinerKey(_eventStore, _otherTenant));

            for (var count = 0; count < 20; count++)
            {
                await firstMiner.Mine([extractor.Extract(EventFactory.EventFor(_eventStore, _firstTenant, Scope, "RegisterInvoice", "Invoice", new DateTimeOffset(2026, 8, 24, 6, 30, 0, TimeSpan.Zero)))]);
                await otherMiner.Mine([extractor.Extract(EventFactory.EventFor(_eventStore, _otherTenant, Scope, "SubmitTimesheet", "Timesheet", new DateTimeOffset(2026, 8, 28, 18, 30, 0, TimeSpan.Zero)))]);
            }

            await firstMiner.Persist();
            await otherMiner.Persist();

            var storage = Services.GetRequiredService<IStorage>();
            var storedForFirstTenant = (await storage.GetEventStore(_eventStore).GetNamespace(_firstTenant).Patterns.GetForScope(Scope)).ToArray();
            var storedForOtherTenant = (await storage.GetEventStore(_eventStore).GetNamespace(_otherTenant).Patterns.GetForScope(Scope)).ToArray();

            BothTenantsEstablishedBehavior = storedForFirstTenant.Length > 0 && storedForOtherTenant.Length > 0;
            OtherTenantBehaviorLeakedIntoTheFirst = storedForFirstTenant.Any(pattern => pattern.Action.Value == "SubmitTimesheet");
            FirstTenantBehaviorLeakedIntoTheOther = storedForOtherTenant.Any(pattern => pattern.Action.Value == "RegisterInvoice");
            OnlyTheTenantOwnObservationsCounted = storedForFirstTenant.Concat(storedForOtherTenant).All(pattern => pattern.Occurrences.Value == 20L);
        }
    }

    [Fact] void should_establish_behavior_for_both_tenants() => Context.BothTenantsEstablishedBehavior.ShouldBeTrue();
    [Fact] void should_not_leak_the_other_tenant_behavior_into_the_first() => Context.OtherTenantBehaviorLeakedIntoTheFirst.ShouldBeFalse();
    [Fact] void should_not_leak_the_first_tenant_behavior_into_the_other() => Context.FirstTenantBehaviorLeakedIntoTheOther.ShouldBeFalse();
    [Fact] void should_count_only_the_tenant_own_observations() => Context.OnlyTheTenantOwnObservationsCounted.ShouldBeTrue();
}
