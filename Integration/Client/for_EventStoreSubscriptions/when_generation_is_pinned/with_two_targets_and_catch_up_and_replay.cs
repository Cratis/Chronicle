// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned.with_two_targets_and_catch_up_and_replay.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

[Collection(ChronicleCollection.Name)]
public class with_two_targets_and_catch_up_and_replay(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        const string Source = "pinned-subscription-source";
        const string Current = "pinned-subscription-current";
        const string Legacy = "pinned-subscription-legacy";
        const string CatchUpTarget = "pinned-subscription-catch-up";
        readonly SubscriptionDeliveryTracker _tracker = new();
#pragma warning disable CA2213 // The specification lifecycle invokes Destroy(), which disposes this client.
        ChronicleClient _legacyClient;
#pragma warning restore CA2213
        public (string Name, EventContext Context) CurrentDelivery;
        public (string Name, EventContext Context) LegacyDelivery;
        public (string Name, EventContext Context) CatchUpDelivery;
        public (string Name, EventContext Context) ReplayDelivery;
        public EventStoreSubscriptionDefinition Definition;
        public override IEnumerable<Type> EventTypes => [typeof(SubscriptionPersonRegistered), typeof(SubscriptionPersonRegisteredV1)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(SubscriptionPersonMigration)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(_tracker);

        async Task Because()
        {
            var source = await ChronicleClient.GetEventStore(Source);
            var current = await ChronicleClient.GetEventStore(Current);

            // The legacy target only knows generation 1. A typed reactor cannot declare an earlier generation's CLR type
            // as its handler, so it observes through a client that pins generation 1 for its own observers.
            _legacyClient = new ChronicleClient(
                new BorrowedConnection(EventStore.Connection),
                new ChronicleOptions
                {
                    EventGenerationDelivery = EventGenerationDelivery.Pinned,
                    DefaultSinkTypeId = Services.GetRequiredService<IChronicleClient>().Options.DefaultSinkTypeId
                },
                artifactsProvider: this,
                serviceProvider: Services);
            var legacy = await _legacyClient.GetEventStore(Legacy);
            await source.RegisterAll();
            await current.RegisterAll();
            await legacy.RegisterAll();
            var currentReactor = await current.Reactors.Register<CurrentInboxReactor>();
            var legacyReactor = await legacy.Reactors.Register(
                "legacy-inbox-reactor",
                reactor => reactor
                    .OnEventSequence($"{EventSequenceId.InboxPrefix}{Source}")
                    .WithEventType(legacy.EventTypes.GetEventTypeFor(typeof(SubscriptionPersonRegisteredV1))),
                (@event, _) => _tracker.Delivered(@event.Content.Single(_ => string.Equals(_.Key, nameof(SubscriptionPersonRegisteredV1.Name), StringComparison.OrdinalIgnoreCase)).Value!.GetValue<string>(), @event.Context));
            await currentReactor.WaitTillActive();
            await legacyReactor.WaitTillActive();
            await current.Subscriptions.Subscribe("current", Source, builder => builder.WithEventType<SubscriptionPersonRegistered>().WithGenerationDelivery(EventGenerationDelivery.Pinned));
            await legacy.Subscriptions.Subscribe("legacy", Source, builder => builder.WithEventType<SubscriptionPersonRegisteredV1>().WithGenerationDelivery(EventGenerationDelivery.Pinned));

            var appended = await source.GetEventSequence(EventSequenceId.Outbox).Append("person", new SubscriptionPersonRegistered("Ada Lovelace"));
            (await appended.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
            CurrentDelivery = await _tracker.Next(Current);
            LegacyDelivery = await _tracker.Next(Legacy);
            Definition = (await current.Subscriptions.GetAll()).Single(_ => _.Id.Value == "current");

            var catchUp = await ChronicleClient.GetEventStore(CatchUpTarget);
            await catchUp.RegisterAll();
            var catchUpReactor = await catchUp.Reactors.Register<CurrentInboxReactor>();
            await catchUpReactor.WaitTillActive();
            await catchUp.Subscriptions.Subscribe("catch-up", Source, builder => builder.WithEventType<SubscriptionPersonRegistered>().WithGenerationDelivery(EventGenerationDelivery.Pinned));
            CatchUpDelivery = await _tracker.Next(CatchUpTarget);

            var replay = await ((IChronicleServicesAccessor)source.Connection).Services.Observers.Replay(new()
            {
                EventStore = Source, Namespace = source.Namespace, EventSequenceId = EventSequenceId.Outbox, ObserverId = $"current->{Current}"
            });
            ReplayDelivery = await _tracker.Next(Current);
            await source.Jobs.WaitTillJobCompletesOrIsDeleted(new JobId(Guid.Parse(replay.JobId)), TimeSpan.FromSeconds(30));
        }

        void Destroy()
        {
            _legacyClient?.EvictEventStores();
            _legacyClient?.Dispose();
        }
    }

    [Fact] void should_keep_the_generic_generation_through_registration() => Context.Definition.EventTypePins.Single().Generation.Value.ShouldEqual(2u);
    [Fact] void should_store_the_pinned_delivery_policy() => Context.Definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_append_generation_two_in_the_current_targets_inbox() => Context.CurrentDelivery.Context.AppendedGeneration!.Value.ShouldEqual(2u);
    [Fact] void should_append_generation_one_in_the_legacy_targets_inbox() => Context.LegacyDelivery.Context.AppendedGeneration!.Value.ShouldEqual(1u);
    [Fact] void should_deliver_generation_one_to_the_legacy_target() => Context.LegacyDelivery.Context.EventType.Generation.Value.ShouldEqual(1u);
    [Fact] void should_release_and_reprotect_personal_data_for_the_current_target() => Context.CurrentDelivery.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_release_and_reprotect_personal_data_for_the_legacy_target() => Context.LegacyDelivery.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_preserve_the_same_pin_on_catch_up() => Context.CatchUpDelivery.Context.AppendedGeneration.ShouldEqual(Context.CurrentDelivery.Context.AppendedGeneration);
    [Fact] void should_preserve_the_same_content_on_catch_up() => Context.CatchUpDelivery.Name.ShouldEqual(Context.CurrentDelivery.Name);
    [Fact] void should_preserve_the_same_pin_on_replay() => Context.ReplayDelivery.Context.AppendedGeneration.ShouldEqual(Context.CurrentDelivery.Context.AppendedGeneration);
    [Fact] void should_preserve_the_same_content_on_replay() => Context.ReplayDelivery.Name.ShouldEqual(Context.CurrentDelivery.Name);
}
