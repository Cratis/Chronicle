// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patterns;
using Cratis.Chronicle.Supervision;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.given;

public class a_pattern_miner : Specification
{
    protected static readonly DateTimeOffset Occurred = new(2026, 8, 24, 9, 15, 0, TimeSpan.Zero);

    protected TestKitSilo _silo;
    protected PatternMiner _miner;
    protected ISiloSupervisor _supervisor;
    protected ManualTimeProvider _clock;
    protected Func<Task<TimeSpan?>>? _check;
    protected TimeSpan? _checkInterval;
    protected IBehaviorPatternStorage _patterns;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    async Task Establish()
    {
        _eventStore = "some-store";
        _namespace = EventStoreNamespaceName.Default;

        _patterns = Substitute.For<IBehaviorPatternStorage>();
        _patterns.GetForScope(Arg.Any<PatternGroupingKey>()).Returns([]);
        _clock = new ManualTimeProvider(Occurred);
        _supervisor = Substitute.For<ISiloSupervisor>();
        _supervisor
            .When(supervisor => supervisor.Register(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<Func<Task<TimeSpan?>>?>(), Arg.Any<Func<Task>?>()))
            .Do(call =>
            {
                _checkInterval = call.ArgAt<TimeSpan?>(1);
                _check = call.ArgAt<Func<Task<TimeSpan?>>?>(2);
            });
        (_silo, _miner) = await CreateMiner(_eventStore, _namespace, _patterns, _supervisor, _clock);
    }

    /// <summary>
    /// Runs the persistence check the miner registered with the supervisor, as the supervisor would when it is due.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task RunSupervisorCheck() => await _check!();

    protected static async Task<(TestKitSilo Silo, PatternMiner Miner)> CreateMiner(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IBehaviorPatternStorage patterns,
        ISiloSupervisor? supervisor = null,
        TimeProvider? clock = null)
    {
        var options = Options.Create(new ChronicleOptions
        {
            PatternDetection = new PatternDetection
            {
                Error = 0.001d,
                MinimumSupport = 0.1d,
                MinimumConfidence = 0.5d,
                DecayFactor = 1d
            }
        });

        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        namespaceStorage.Patterns.Returns(patterns);

        var silo = new TestKitSilo();
        silo.AddService<IFacetVocabulary>(new FacetVocabulary(options));
        silo.AddService<IFacetSetGenerator>(new FacetSetGenerator());
        silo.AddService(storage);
        silo.AddService(options);
        silo.AddService(Substitute.For<ILogger<PatternMiner>>());
        silo.AddService(supervisor ?? Substitute.For<ISiloSupervisor>());
        silo.AddService(clock ?? TimeProvider.System);

        var miner = await silo.CreateGrainAsync<PatternMiner>(new PatternMinerKey(eventStore, @namespace).ToString());
        silo.AddProbe<IPatternMiner>(_ => miner);
        return (silo, miner);
    }

    /// <summary>
    /// Represents a <see cref="TimeProvider"/> whose time only moves when told to.
    /// </summary>
    /// <param name="start">The time it starts at.</param>
    protected class ManualTimeProvider(DateTimeOffset start) : TimeProvider
    {
        DateTimeOffset _now = start;

        /// <summary>
        /// Moves the time forward.
        /// </summary>
        /// <param name="by">How far.</param>
        public void Advance(TimeSpan by) => _now += by;

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => _now;
    }

    protected static EventFeatures Features(
        string groupingKey,
        string commandType,
        DayOfWeek day = DayOfWeek.Monday,
        TimeBucket timeBucket = TimeBucket.Morning,
        DateTimeOffset? occurred = null) =>
        new(
            groupingKey,
            commandType,
            InitiatorType.User,
            groupingKey,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            "ExpenseReport",
            2026,
            8,
            day,
            timeBucket,
            occurred ?? Occurred);
}
