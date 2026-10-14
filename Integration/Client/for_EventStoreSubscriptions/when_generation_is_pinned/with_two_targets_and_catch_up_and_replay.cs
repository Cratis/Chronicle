// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
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
            var legacy = await ChronicleClient.GetEventStore(Legacy);
            await source.RegisterAll();
            await current.RegisterAll();
            await legacy.RegisterAll();
            var currentReactor = await current.Reactors.Register<CurrentInboxReactor>();
            var legacyReactor = await legacy.Reactors.Register<LegacyInboxReactor>();
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
    }

    [Fact] void should_keep_the_generic_generation_through_registration() => Context.Definition.EventTypePins.Single().Generation.Value.ShouldEqual(2u);
    [Fact] void should_store_the_pinned_delivery_policy() => Context.Definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_append_generation_two_in_the_current_targets_inbox() => Context.CurrentDelivery.Context.AppendedGeneration!.Value.ShouldEqual(2u);
    [Fact] void should_append_generation_one_in_the_legacy_targets_inbox() => Context.LegacyDelivery.Context.AppendedGeneration!.Value.ShouldEqual(1u);
    [Fact] void should_release_and_reprotect_personal_data_for_the_current_target() => Context.CurrentDelivery.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_release_and_reprotect_personal_data_for_the_legacy_target() => Context.LegacyDelivery.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_preserve_the_same_pin_on_catch_up() => Context.CatchUpDelivery.Context.AppendedGeneration.ShouldEqual(Context.CurrentDelivery.Context.AppendedGeneration);
    [Fact] void should_preserve_the_same_content_on_catch_up() => Context.CatchUpDelivery.Name.ShouldEqual(Context.CurrentDelivery.Name);
    [Fact] void should_preserve_the_same_pin_on_replay() => Context.ReplayDelivery.Context.AppendedGeneration.ShouldEqual(Context.CurrentDelivery.Context.AppendedGeneration);
    [Fact] void should_preserve_the_same_content_on_replay() => Context.ReplayDelivery.Name.ShouldEqual(Context.CurrentDelivery.Name);
}
