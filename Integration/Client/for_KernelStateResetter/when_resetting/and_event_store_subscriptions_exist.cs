// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.DevelopmentTools;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using context = Cratis.Chronicle.Integration.for_KernelStateResetter.when_resetting.and_event_store_subscriptions_exist.context;

namespace Cratis.Chronicle.Integration.for_KernelStateResetter.when_resetting;

[Collection(ChronicleCollection.Name)]
public class and_event_store_subscriptions_exist(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : for_EventStoreSubscriptions.given.multi_event_store_subscription_setup(fixture)
    {
        public IEnumerable<EventStoreSubscriptionDefinition> SubscriptionsAfterReset { get; private set; } = [];
        public Concepts.Events.EventSequenceNumber InboxAfterReset { get; private set; } = 0;

        public override IEnumerable<Type> EventTypes => [typeof(ForwardedEvent)];

        async Task Establish()
        {
            SourceEventStoreName = "reset-source";
            TargetEventStoreName = "reset-target";
            await Subscribe("old-subscription", SourceEventStoreName, TargetEventStoreName);
            await AppendToOutbox("before-reset");
            var tail = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, 0);
            tail.Value.ShouldEqual(0UL);
        }

        async Task Because()
        {
            ChronicleClient.EvictEventStores();
            if (ChronicleFixture.Options.Mode == ChronicleRuntimeMode.OutOfProcess)
            {
                await ChronicleFixture.ResetForLifecycleProofAsync();
            }
            else
            {
                await Services.GetRequiredService<IKernelStateResetter>().Reset();
            }

            var source = await ChronicleClient.GetEventStore(SourceEventStoreName);
            await source.RegisterAll();
            var target = await ChronicleClient.GetEventStore(TargetEventStoreName);
            await target.RegisterAll();
            SubscriptionsAfterReset = await GetStoredSubscriptions(TargetEventStoreName);

            await AppendToOutbox("after-reset");
            InboxAfterReset = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, 0, TimeSpan.FromSeconds(2));
        }

        async Task AppendToOutbox(string value)
        {
            var source = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var result = await source.GetEventSequence(EventSequenceId.Outbox).Append(new EventSourceId(value), new ForwardedEvent(value));
            result.IsSuccess.ShouldBeTrue();
        }
    }

    [EventType]
    [Public]
    public record ForwardedEvent(string Value);

    [Fact] void should_remove_the_persisted_subscription() => Context.SubscriptionsAfterReset.ShouldBeEmpty();
    [Fact] void should_not_forward_events_after_reset() => Context.InboxAfterReset.ShouldEqual(Concepts.Events.EventSequenceNumber.Unavailable);
}
