// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;

using context = Cratis.Chronicle.Integration.for_Telemetry.when_replaying_with_default_orleans_tracing.and_the_kernel_span_count_is_bounded.context;

namespace Cratis.Chronicle.Integration.for_Telemetry.when_replaying_with_default_orleans_tracing;

/// <summary>
/// CI-only: uses the real kernel and MongoDB. Never run as a local unit specification.
/// </summary>
[Collection(nameof(given.replay_telemetry_collection))]
public class and_the_kernel_span_count_is_bounded(context setup, ITestOutputHelper output) : Given<context>(setup)
{
    public class context(ChronicleInProcessFixture fixture) : Specification<ChronicleInProcessFixture>(fixture)
    {
        public const int EventCount = 100;

        /// <summary>
        /// Budget, not a performance claim: CI must calibrate the observed count before shipping.
        /// Two observers replay 100 events each; allow 12 kernel spans/event plus 300 fixed job spans.
        /// </summary>
        public const int MaximumSpans = (12 * EventCount) + 300;
        public given.replay_span_collector Collector;
        public Reactor Reactor;
        public ReadModel Live;
        public ReadModel Replayed;
        TracerProvider _provider;

        public override IEnumerable<Type> EventTypes => [typeof(EventApplied)];
        public override IEnumerable<Type> Projections => [typeof(Projection)];
        public override IEnumerable<Type> Reactors => [typeof(Reactor)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
            services.AddSingleton(new Projection());
        }

        protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
            base.ConfigureWebHostBuilder(builder);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Telemetry:OrleansTracing"] = "false"
            }));
        }

        void Establish()
        {
            Collector = new();
            var builder = Sdk.CreateTracerProviderBuilder();
            builder.AddChronicleInstrumentation();
            _provider = builder.AddProcessor(new SimpleActivityExportProcessor(Collector)).Build();
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetAllHandlers().Single();
            await projection.WaitTillActive();
            await EventStore.Reactors.GetHandlerFor<Reactor>().WaitTillActive();
            EventSourceId source = "replay-telemetry";
            var lastSequenceNumber = EventSequenceNumber.Unavailable;
            for (var value = 1; value <= EventCount; value++)
                lastSequenceNumber = (await EventStore.EventLog.Append(source, new EventApplied(value))).SequenceNumber;
            await Reactor.WaitFor(EventCount);
            await projection.WaitTillReachesEventSequenceNumber(lastSequenceNumber);
            Live = await EventStore.ReadModels.GetInstanceById<ReadModel>(source.Value);
            Reactor.Reset();
            Collector.Spans.Clear();

            var projectionJob = await EventStore.Projections.Replay<Projection>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(projectionJob);
            await projection.WaitTillActive();
            var reactorJob = await EventStore.Reactors.Replay<Reactor>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(reactorJob);
            await Reactor.WaitFor(EventCount);
            Replayed = await EventStore.ReadModels.GetInstanceById<ReadModel>(source.Value);
            _provider.ForceFlush();
        }

        void Destroy() => _provider.Dispose();
    }

    [Fact] void should_build_the_live_read_model() => Context.Live.Value.ShouldEqual(context.EventCount);
    [Fact] void should_replay_the_projection_correctly() => Context.Replayed.Value.ShouldEqual(Context.Live.Value);
    [Fact] void should_replay_every_event_in_the_reactor() => Context.Reactor.Handled.ShouldEqual(context.EventCount);
    [Fact] void should_include_kernel_observer_spans() => Context.Collector.Spans.Any(span => span.OperationName == "cratis.chronicle.observer.handle").ShouldBeTrue();
    [Fact] void should_not_export_orleans_spans() => Context.Collector.Spans.Any(span => OrleansActivitySources.All.Contains(span.Source.Name)).ShouldBeFalse();

    [Fact]
    void should_bound_the_replay_span_count()
    {
        var count = Context.Collector.Spans.Count;
        output.WriteLine($"Replaying {context.EventCount} events through a projection and reactor exported {count} kernel spans (budget {context.MaximumSpans}).");
        count.ShouldBeGreaterThan(0);
        Assert.True(count <= context.MaximumSpans, $"Replay exported {count} spans; budget is {context.MaximumSpans}.");
    }
}
