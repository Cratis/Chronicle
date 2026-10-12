// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

using context = Cratis.Chronicle.Integration.for_ExplicitArtifacts.when_registering_plain_artifacts.and_the_options_nominate_unmarked_types.context;

namespace Cratis.Chronicle.Integration.for_ExplicitArtifacts.when_registering_plain_artifacts;

[Collection(ChronicleCollection.Name)]
public class and_the_options_nominate_unmarked_types(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture infrastructure) : Specification<ExplicitArtifactsFixture>(new(infrastructure))
    {
        public const string EventId = "explicit-integration-order-placed";
        public const string ReactorId = "explicit-integration-order-notifier";
        public const string ReducerId = "explicit-integration-order-total";

        readonly TaskCompletionSource<(OrderPlaced Event, EventContext Context)> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId Source { get; } = EventSourceId.New();
        public RegistrationOutcome Registration { get; private set; } = default!;
        public bool AppendSucceeded { get; private set; }
        public IReactorHandler Reactor { get; private set; } = default!;
        public IReducerHandler Reducer { get; private set; } = default!;
        public OrderPlaced ReceivedEvent { get; private set; } = default!;
        public EventContext ReceivedContext { get; private set; } = default!;
        public OrderTotal Total { get; private set; } = default!;

        protected override void ConfigureServices(IServiceCollection services)
        {
            // Options.Create bypasses the host's PostConfigure pipeline, so preserve its storage-specific sink.
            var options = new ChronicleOptions
            {
                DefaultSinkTypeId = GetDefaultSinkTypeId() ?? Sinks.WellKnownSinkTypes.MongoDB
            };
            options.ExplicitArtifacts
                .RegisterEventType<OrderPlaced>(_ => _.WithId(EventId))
                .RegisterReactor<OrderNotifier>(ReactorId)
                .RegisterReducer<OrderTotalReducer>(ReducerId);
            services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(options));
            services.AddSingleton(new OrderNotifier(_received));
        }

        /// <inheritdoc/>
        protected override Action<Configuration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer)
        {
            if (infrastructure.Options.Mode != ChronicleRuntimeMode.OutOfProcess) return null;
            if (infrastructure.Options.StorageProvider == ChronicleStorageProvider.MongoDB)
            {
                var server = $"mongodb://localhost:{infrastructure.MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true";
                return builder => Setup.MongoDBChronicleBuilderExtensions.WithMongoDB(builder, server, infrastructure.InProcessMongoDatabaseName);
            }
            if (infrastructure.InProcessStorageType is null) return null;
            var options = new Configuration.ChronicleOptions
            {
                Storage = new Configuration.Storage { Type = infrastructure.InProcessStorageType, ConnectionDetails = infrastructure.GetInProcessConnectionString() }
            };
            return builder => Setup.SqlChronicleBuilderExtensions.WithSql(builder, options);
        }

        protected override IReadOnlyDictionary<string, string?>? GetStorageHostConfiguration(string mongoServer) =>
            infrastructure.Options.Mode == ChronicleRuntimeMode.OutOfProcess &&
            infrastructure.Options.StorageProvider != ChronicleStorageProvider.MongoDB && infrastructure.InProcessStorageType is not null
                ? new Dictionary<string, string?>
                {
                    ["Cratis:Chronicle:Storage:Type"] = infrastructure.InProcessStorageType,
                    ["Cratis:Chronicle:Storage:ConnectionDetails"] = infrastructure.GetInProcessConnectionString()
                }
                : null;

        protected override Sinks.SinkTypeId? GetDefaultSinkTypeId() =>
            infrastructure.Options.Mode == ChronicleRuntimeMode.OutOfProcess && infrastructure.Options.StorageProvider != ChronicleStorageProvider.MongoDB
                ? Sinks.WellKnownSinkTypes.SQL
                : null;

        async Task Because()
        {
            Registration = await EventStore.WaitForRegistration(TimeSpanFactory.DefaultTimeout());
            Reactor = EventStore.Reactors.GetHandlerFor(typeof(OrderNotifier));
            Reducer = EventStore.Reducers.GetHandlerFor(typeof(OrderTotalReducer));
            await Reactor.WaitTillSubscribed();
            await Reducer.WaitTillSubscribed();
            var append = await EventStore.EventLog.Append(Source, new OrderPlaced(42));
            AppendSucceeded = append.IsSuccess;
            (ReceivedEvent, ReceivedContext) = await _received.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await Reducer.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            Total = await EventStore.ReadModels.GetInstanceById<OrderTotal>(Source);
        }
    }

    [Fact] void should_disable_assembly_discovery() => Context.AutoDiscoverArtifacts.ShouldBeFalse();
    [Fact] void should_complete_registration_without_failure() => Context.Registration.Failure.ShouldBeNull();
    [Fact] void should_append_successfully_to_the_real_kernel() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_register_the_explicit_event_identity() => Context.EventStore.EventTypes.GetEventTypeFor(typeof(OrderPlaced)).Id.Value.ShouldEqual(context.EventId);
    [Fact] void should_register_the_explicit_reactor_identity() => Context.Reactor.Id.Value.ShouldEqual(context.ReactorId);
    [Fact] void should_register_the_explicit_reducer_identity() => Context.Reducer.Id.Value.ShouldEqual(context.ReducerId);
    [Fact] void should_deliver_the_event_to_the_plain_reactor() => Context.ReceivedEvent.ShouldEqual(new OrderPlaced(42));
    [Fact] void should_deliver_with_the_explicit_event_identity() => Context.ReceivedContext.EventType.Id.Value.ShouldEqual(context.EventId);
    [Fact] void should_preserve_the_event_source() => Context.ReceivedContext.EventSourceId.ShouldEqual(Context.Source);
    [Fact] void should_materialize_the_plain_reducers_state() => Context.Total.Amount.ShouldEqual(42);

    public record OrderPlaced(int Amount);
    public record OrderTotal(int Amount);

    public class OrderNotifier(TaskCompletionSource<(OrderPlaced Event, EventContext Context)> received)
    {
        public void On(OrderPlaced @event, EventContext eventContext) => received.TrySetResult((@event, eventContext));
    }

    public class OrderTotalReducer
    {
        public OrderTotal Reduce(OrderPlaced @event, OrderTotal? current) => new((current?.Amount ?? 0) + @event.Amount);
    }
}
