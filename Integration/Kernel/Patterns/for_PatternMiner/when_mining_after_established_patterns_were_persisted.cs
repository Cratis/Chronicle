// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using context = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_after_established_patterns_were_persisted.context;
using EventFactory = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_behavior_for_a_scope.context;

namespace Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner;

/// <summary>
/// The life after a restart, over real storage: the store already holds a scope's established behavior while the
/// miner activation starts empty. Its first mine for the scope must restore what was established and continue
/// counting - a fresh sketch would hold its first events with full support, and the flush would rewrite the scope
/// from that, wiping the established behavior.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_mining_after_established_patterns_were_persisted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public const string Scope = "ingrid.holm";

        public bool EstablishedBehaviorKept;
        public long EstablishedOccurrences;
        public double EstablishedConfidence;

        readonly ConceptsEventStoreName _eventStore = $"patterns-{Guid.NewGuid():N}";
        readonly ConceptsEventStoreNamespaceName _establishedIn = "before-restart";
        readonly ConceptsEventStoreNamespaceName _restoredInto = "after-restart";

        async Task Establish()
        {
            // The life before the restart establishes behavior organically and persists it; its surviving
            // patterns are copied to the other namespace's storage, whose own miner has never been activated -
            // exactly the state a restart leaves behind.
            var extractor = Services.GetRequiredService<IEventFeatureExtractor>();
            var establishedMiner = Services.GetRequiredService<IGrainFactory>()
                .GetGrain<IPatternMiner>(new PatternMinerKey(_eventStore, _establishedIn));

            for (var count = 0; count < 20; count++)
            {
                var features = extractor.Extract(EventFactory.EventFor(_eventStore, _establishedIn, Scope, "RegisterInvoice", "Invoice", new DateTimeOffset(2026, 8, 24, 6, 30, 0, TimeSpan.Zero)));
                await establishedMiner.Mine([features]);
            }

            await establishedMiner.Persist();

            var storage = Services.GetRequiredService<IStorage>();
            var established = await storage.GetEventStore(_eventStore).GetNamespace(_establishedIn).Patterns.GetForScope(Scope);
            await storage.GetEventStore(_eventStore).GetNamespace(_restoredInto).Patterns.Save(established);
        }

        async Task Because()
        {
            var extractor = Services.GetRequiredService<IEventFeatureExtractor>();
            var restoredMiner = Services.GetRequiredService<IGrainFactory>()
                .GetGrain<IPatternMiner>(new PatternMinerKey(_eventStore, _restoredInto));

            var features = extractor.Extract(EventFactory.EventFor(_eventStore, _restoredInto, Scope, "AnswerPayrollQuery", "PayrollQuery", new DateTimeOffset(2026, 8, 28, 14, 30, 0, TimeSpan.Zero)));
            await restoredMiner.Mine([features]);
            await restoredMiner.Persist();

            var stored = (await Services.GetRequiredService<IStorage>()
                .GetEventStore(_eventStore)
                .GetNamespace(_restoredInto)
                .Patterns
                .GetForScope(Scope)).ToArray();

            var establishedCombination = stored.SingleOrDefault(pattern =>
                pattern.Action.Value == "RegisterInvoice" &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "Day") &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "TimeBucket"));

            EstablishedBehaviorKept = establishedCombination is not null;
            EstablishedOccurrences = establishedCombination?.Occurrences.Value ?? 0L;
            EstablishedConfidence = establishedCombination?.Confidence.Value ?? 0d;
        }
    }

    [Fact] void should_keep_the_established_behavior() => Context.EstablishedBehaviorKept.ShouldBeTrue();
    [Fact] void should_keep_the_established_counts() => Context.EstablishedOccurrences.ShouldEqual(20L);
    [Fact] void should_keep_the_established_confidence() => Context.EstablishedConfidence.ShouldEqual(1d);
}
