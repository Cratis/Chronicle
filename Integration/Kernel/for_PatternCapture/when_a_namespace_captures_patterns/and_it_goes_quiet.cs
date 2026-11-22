// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using context = Cratis.Chronicle.Kernel.Integration.for_PatternCapture.when_a_namespace_captures_patterns.and_it_goes_quiet.context;
using EventFactory = Cratis.Chronicle.Kernel.Integration.Patterns.for_PatternMiner.when_mining_behavior_for_a_scope.context;

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.when_a_namespace_captures_patterns;

/// <summary>
/// Pattern capture is on by default (#6118): the namespace gets a capture observer that observes every event type and a miner
/// that persists what it has mined. Once the namespace is quiet neither of them stays resident: the miner leaves when a whole
/// persistence interval passes without mining, and the capture observer is released like any dormant observer and woken by
/// the next event.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.PatternCaptureCollection.Name)]
public class and_it_goes_quiet(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_pattern_capturing_namespace(fixture)
    {
        public bool CaptureWasRegistered;
        public bool MinerWasResident;
        public bool MinedPatternsWerePersisted;
        public bool MinerReleasedWhenIdle;
        public bool CaptureObserverReleasedWhenQuiet;

        async Task Because()
        {
            await _store.EventLog.Append("source", new for_DormantObserver.given.DormantObserverEvent("first"));
            CaptureWasRegistered = await WaitUntil(CaptureIsRegistered, TimeSpan.FromSeconds(60));

            var extractor = Services.GetRequiredService<IEventFeatureExtractor>();
            var miner = Services.GetRequiredService<IGrainFactory>().GetGrain<IPatternMiner>(new PatternMinerKey(_store.Name.Value, ConceptsEventStoreNamespaceName.Default));
            for (var count = 0; count < 20; count++)
            {
                await miner.Mine([extractor.Extract(EventFactory.EventFor(_store.Name.Value, ConceptsEventStoreNamespaceName.Default, "ingrid.holm", "RegisterInvoice", "Invoice", new DateTimeOffset(2026, 8, 24, 6, 30, 0, TimeSpan.Zero)))]);
            }

            MinerWasResident = (await ResidentMiners()).Length > 0;

            // The persistence interval is two seconds, so what was mined is stored without anybody asking for it.
            var patterns = Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).GetNamespace(ConceptsEventStoreNamespaceName.Default).Patterns;
            MinedPatternsWerePersisted = await WaitUntil(async () => (await patterns.GetForScope("ingrid.holm")).Any(), TimeSpan.FromSeconds(30));

            MinerReleasedWhenIdle = await WaitUntil(async () => (await ResidentMiners()).Length == 0, TimeSpan.FromSeconds(30));
            CaptureObserverReleasedWhenQuiet = await WaitUntil(async () => (await ResidentCaptureObservers()).Length == 0, TimeSpan.FromSeconds(150));
        }
    }

    [Fact] void should_have_registered_the_capture_for_the_event_store() => Context.CaptureWasRegistered.ShouldBeTrue();
    [Fact] void should_have_activated_the_miner() => Context.MinerWasResident.ShouldBeTrue();
    [Fact] void should_persist_what_was_mined() => Context.MinedPatternsWerePersisted.ShouldBeTrue();

    [Fact] void should_release_the_miner_when_idle() => Context.MinerReleasedWhenIdle.ShouldBeTrue();
    [Fact] void should_release_the_capture_observer_when_quiet() => Context.CaptureObserverReleasedWhenQuiet.ShouldBeTrue();
}
