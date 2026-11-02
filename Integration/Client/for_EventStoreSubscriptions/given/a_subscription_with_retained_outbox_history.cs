// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

public class a_subscription_with_retained_outbox_history(ChronicleFixture fixture) : Specification(fixture)
{
    protected const string SubscriptionId = "history-transfer";
    protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);
    protected IEventStore Source;
    protected IEventStore Consumer;
    protected EventSequenceId InboxId;
    protected IObserver Forwarder;
    protected IAppendResultForObserverCompletion LastSeed;
    public IReadOnlyList<AppendedEvent> StoredEvents;
    public EventSequenceNumber FirstSeedNumber;
    readonly Channel<string> _deliveries = Channel.CreateUnbounded<string>();
    IReactorHandler _inboxReactor;

    public override IEnumerable<Type> EventTypes => [typeof(SubscriptionHistoryPublished)];

    async Task Establish()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ns = $"history-{suffix}";
        Source = await ChronicleClient.GetEventStore($"producer-{suffix}", ns);
        Consumer = await ChronicleClient.GetEventStore($"consumer-{suffix}", ns);
        await Source.RegisterAll();
        await Consumer.RegisterAll();
        InboxId = $"{EventSequenceId.InboxPrefix}{Source.Name}";
        _inboxReactor = await Consumer.Reactors.Register($"history-receipts-{suffix}", builder => builder
            .On<SubscriptionHistoryPublished>(@event => _deliveries.Writer.TryWrite(@event.Value))
            .OnEventSequence(InboxId));
        await _inboxReactor.WaitTillActive(Timeout);
        var key = new Concepts.Observation.ObserverKey($"{SubscriptionId}->{Consumer.Name}", Source.Name.Value, ns, Concepts.EventSequences.EventSequenceId.Outbox);
        Forwarder = Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(key.ToString());
        var first = await Source.GetEventSequence(EventSequenceId.Outbox).Append("first-source", new SubscriptionHistoryPublished("first"));
        first.IsSuccess.ShouldBeTrue();
        FirstSeedNumber = first.SequenceNumber;
        var last = await Source.GetEventSequence(EventSequenceId.Outbox).Append("second-source", new SubscriptionHistoryPublished("second"));
        last.IsSuccess.ShouldBeTrue();
        LastSeed = last;
    }

    protected async Task SubscribeAndDeliverHistory()
    {
        await Subscribe();
        await WaitForDeliveries(2);
        (await LastSeed.WaitForCompletion(Timeout)).IsSuccess.ShouldBeTrue();
        await WaitForForwarderCheckpoint(LastSeed.TailSequenceNumber.Value);
    }

    protected Task Subscribe() => Consumer.Subscriptions.Subscribe(SubscriptionId, Source.Name, builder => builder.WithEventType<SubscriptionHistoryPublished>());

    protected async Task WaitForDeliveries(int count)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        for (var index = 0; index < count; index++)
        {
            await _deliveries.Reader.ReadAsync(cancellation.Token);
        }
        var tail = await Consumer.GetEventSequence(InboxId).GetTailSequenceNumber();
        await _inboxReactor.WaitTillReachesEventSequenceNumber(tail, Timeout);
    }

    protected async Task ReadInbox() => StoredEvents = await Consumer.GetEventSequence(InboxId).GetFromSequenceNumber(0);

    protected async Task WaitForForwarderCheckpoint(ulong expected)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        while ((await Forwarder.GetState()).LastHandledEventSequenceNumber.Value != expected)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
        }
    }

    public IEnumerable<string> StoredValues() => StoredEvents.Select(_ => ((SubscriptionHistoryPublished)_.Content).Value);
}
