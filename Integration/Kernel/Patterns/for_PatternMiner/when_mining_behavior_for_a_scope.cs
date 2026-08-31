// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using context = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_behavior_for_a_scope.context;

namespace Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner;

/// <summary>
/// The whole mining path, end to end through a real silo: an appended event's context goes through the real
/// feature extractor, the features cross a real grain call to the miner keyed by event store and namespace, are
/// counted in its sketch, and the surviving patterns are persisted to the namespace's own storage - which is
/// where the query surface reads them from.
/// <para>
/// The client SDK mirrors the kernel's pattern concept namespaces, so naming those types here is ambiguous - the
/// context works through the extractor, inference, and primitive outcomes instead.
/// </para>
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_mining_behavior_for_a_scope(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public const string Scope = "ingrid.holm";

        public bool FullCombinationSurvivedInTheSketch;
        public bool FullCombinationPersisted;
        public long FullCombinationOccurrences;
        public double FullCombinationConfidence;
        public bool EveryPersistedPatternScoped;

        readonly ConceptsEventStoreName _eventStore = $"patterns-{Guid.NewGuid():N}";
        readonly ConceptsEventStoreNamespaceName _namespace = "some-namespace";

        IPatternMiner _miner = default!;

        void Establish() =>
            _miner = Services.GetRequiredService<IGrainFactory>()
                .GetGrain<IPatternMiner>(new PatternMinerKey(_eventStore, _namespace));

        async Task Because()
        {
            var extractor = Services.GetRequiredService<IEventFeatureExtractor>();

            for (var count = 0; count < 20; count++)
            {
                var features = extractor.Extract(EventFor(_eventStore, _namespace, Scope, "RegisterInvoice", "Invoice", new DateTimeOffset(2026, 8, 24, 6, 30, 0, TimeSpan.Zero)));
                await _miner.Mine([features]);
            }

            await _miner.Persist();

            var surviving = await _miner.GetSurvivingPatterns(Scope);
            var stored = (await Services.GetRequiredService<IStorage>()
                .GetEventStore(_eventStore)
                .GetNamespace(_namespace)
                .Patterns
                .GetForScope(Scope)).ToArray();

            // The one combination naming the action together with both time facets - the shape the heatmap reads.
            FullCombinationSurvivedInTheSketch = surviving.Any(pattern =>
                pattern.Action.Value == "RegisterInvoice" &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "Day") &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "TimeBucket"));
            var fullCombination = stored.SingleOrDefault(pattern =>
                pattern.Action.Value == "RegisterInvoice" &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "Day") &&
                pattern.Facets.Facets.Any(facet => facet.Name.Value == "TimeBucket"));
            FullCombinationPersisted = fullCombination is not null;
            FullCombinationOccurrences = fullCombination?.Occurrences.Value ?? 0L;
            FullCombinationConfidence = fullCombination?.Confidence.Value ?? 0d;
            EveryPersistedPatternScoped = stored.All(pattern => pattern.GroupingKey.Value == Scope);
        }

        internal static AppendedEvent EventFor(
            ConceptsEventStoreName eventStore,
            ConceptsEventStoreNamespaceName @namespace,
            string scope,
            string eventType,
            string eventSourceType,
            DateTimeOffset occurred)
        {
            // No causation names a command, so the extractor reads the event type as the action - in an
            // event-sourced store the fact that was recorded is itself what happened.
            var eventContext = EventContext.From(
                eventStore,
                @namespace,
                new EventType(eventType, EventTypeGeneration.First),
                new EventSourceType(eventSourceType),
                new EventSourceId(Guid.NewGuid().ToString()),
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.New(),
                occurred: occurred) with
            {
                CausedBy = new Identity(scope, scope)
            };

            return new AppendedEvent(eventContext, new ExpandoObject());
        }
    }

    [Fact] void should_hold_the_full_combination_in_the_sketch() => Context.FullCombinationSurvivedInTheSketch.ShouldBeTrue();
    [Fact] void should_persist_the_full_combination() => Context.FullCombinationPersisted.ShouldBeTrue();
    [Fact] void should_persist_it_with_every_occurrence_counted() => Context.FullCombinationOccurrences.ShouldEqual(20L);
    [Fact] void should_persist_it_fully_confident() => Context.FullCombinationConfidence.ShouldEqual(1d);
    [Fact] void should_scope_every_persisted_pattern() => Context.EveryPersistedPatternScoped.ShouldBeTrue();
}
