// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_reactors_coordinate_tenant_outbox_forwarding.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_reactors_coordinate_tenant_outbox_forwarding(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string SourceEventStoreName = "Admin";
        public const string TargetEventStoreName = "Lobby";
        public const string TenantANamespace = "tenant-a";
        public const string TenantBNamespace = "tenant-b";

        public EventSequenceNumber TenantAOutboxTailAfterTenantAEvent { get; private set; } = EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber TenantAInboxTailAfterTenantAEvent { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber TenantBInboxTailAfterTenantAEvent { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;
        public EventSequenceNumber TenantBOutboxTailAfterTenantBEvent { get; private set; } = EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber TenantAInboxTailAfterTenantBEvent { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber TenantBInboxTailAfterTenantBEvent { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;

        public TenantForwardingTracker Tracker { get; private set; } = null!;

        public override IEnumerable<Type> EventTypes => [typeof(AdminUserInvited)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Tracker = new TenantForwardingTracker();
            services.AddSingleton(Tracker);
        }

        async Task Because()
        {
            var sourceEventStore = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var sourceTenantA = await ChronicleClient.GetEventStore(SourceEventStoreName, TenantANamespace);
            var sourceTenantB = await ChronicleClient.GetEventStore(SourceEventStoreName, TenantBNamespace);
            var targetTenantA = await ChronicleClient.GetEventStore(TargetEventStoreName, TenantANamespace);
            var targetTenantB = await ChronicleClient.GetEventStore(TargetEventStoreName, TenantBNamespace);

            await sourceEventStore.EventTypes.Register();

            var sourceOutboxReactorA = await sourceTenantA.Reactors.Register<SourceOutboxForwardingReactor>();
            var sourceOutboxReactorB = await sourceTenantB.Reactors.Register<SourceOutboxForwardingReactor>();
            var targetReactorA = await targetTenantA.Reactors.Register<LobbyReactor>();
            var targetReactorB = await targetTenantB.Reactors.Register<LobbyReactor>();

            await sourceOutboxReactorA.WaitTillActive();
            await sourceOutboxReactorB.WaitTillActive();
            await targetReactorA.WaitTillActive();
            await targetReactorB.WaitTillActive();

            await targetTenantA.Subscriptions.Subscribe(
                new EventStoreSubscriptionId("admin-subscription"),
                SourceEventStoreName,
                builder => builder.WithEventType<AdminUserInvited>());

            await WaitForSubscription(targetTenantA, TenantANamespace);
            await WaitForSubscription(targetTenantA, TenantBNamespace);

            Tracker.Prepare(TenantANamespace);
            var tenantAAppendResult = await sourceTenantA.EventLog.Append("tenant-a-user", new AdminUserInvited("tenant-a@chronicle.dev"));
            await sourceOutboxReactorA.WaitTillReachesEventSequenceNumber(tenantAAppendResult.SequenceNumber);
            await WaitForInboxTailSequenceNumber(TenantANamespace, Concepts.Events.EventSequenceNumber.First);
            await Tracker.WaitFor(TenantANamespace);

            TenantAOutboxTailAfterTenantAEvent = await GetOutboxTailSequenceNumber(sourceTenantA);
            TenantAInboxTailAfterTenantAEvent = await GetInboxTailSequenceNumber(TenantANamespace);
            TenantBInboxTailAfterTenantAEvent = await GetInboxTailSequenceNumber(TenantBNamespace);

            Tracker.Prepare(TenantBNamespace);
            var tenantBAppendResult = await sourceTenantB.EventLog.Append("tenant-b-user", new AdminUserInvited("tenant-b@chronicle.dev"));
            await sourceOutboxReactorB.WaitTillReachesEventSequenceNumber(tenantBAppendResult.SequenceNumber);
            await WaitForInboxTailSequenceNumber(TenantBNamespace, Concepts.Events.EventSequenceNumber.First);
            await Tracker.WaitFor(TenantBNamespace);

            TenantBOutboxTailAfterTenantBEvent = await GetOutboxTailSequenceNumber(sourceTenantB);
            TenantAInboxTailAfterTenantBEvent = await GetInboxTailSequenceNumber(TenantANamespace);
            TenantBInboxTailAfterTenantBEvent = await GetInboxTailSequenceNumber(TenantBNamespace);
        }

        async Task<EventSequenceNumber> GetOutboxTailSequenceNumber(IEventStore eventStore)
        {
            var outbox = eventStore.GetEventSequence(EventSequenceId.Outbox);
            return await outbox.GetTailSequenceNumber();
        }

        async Task<Concepts.Events.EventSequenceNumber> GetInboxTailSequenceNumber(string targetNamespace)
        {
            var targetStorage = Services.GetRequiredService<IStorage>().GetEventStore(TargetEventStoreName);
            var inboxSequence = targetStorage
                .GetNamespace(targetNamespace)
                .GetEventSequence(new Concepts.EventSequences.EventSequenceId($"inbox-{SourceEventStoreName}"));

            return await inboxSequence.GetTailSequenceNumber();
        }

        async Task WaitForInboxTailSequenceNumber(string targetNamespace, Concepts.Events.EventSequenceNumber expected)
        {
            var timeout = DateTime.UtcNow.Add(TimeSpanFactory.DefaultTimeout());

            while (DateTime.UtcNow < timeout)
            {
                if (await GetInboxTailSequenceNumber(targetNamespace) == expected)
                {
                    return;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException($"Inbox for namespace '{targetNamespace}' did not reach sequence number {expected.Value}.");
        }

        async Task WaitForSubscription(IEventStore targetEventStore, string targetNamespace)
        {
            var subscriptionsReactor = await targetEventStore.Reactors.WaitForHandlerById(
                "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
                TimeSpanFactory.FromSeconds(60));

            var targetStorage = Services.GetRequiredService<IStorage>().GetEventStore(TargetEventStoreName);
            var systemSequence = targetStorage
                .GetNamespace(targetNamespace)
                .GetEventSequence(Concepts.EventSequences.EventSequenceId.System);

            var tailSequenceNumber = (await systemSequence.GetTailSequenceNumber()).Value;
            await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);
        }
    }

    [Fact]
    void should_append_tenant_a_event_to_tenant_a_outbox() =>
        Context.TenantAOutboxTailAfterTenantAEvent.ShouldEqual(EventSequenceNumber.First);

    [Fact]
    void should_forward_tenant_a_event_to_tenant_a_inbox() =>
        Context.TenantAInboxTailAfterTenantAEvent.ShouldEqual(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    void should_not_forward_tenant_a_event_to_tenant_b_inbox() =>
        Context.TenantBInboxTailAfterTenantAEvent.ShouldEqual(Concepts.Events.EventSequenceNumber.Unavailable);

    [Fact]
    void should_append_tenant_b_event_to_tenant_b_outbox() =>
        Context.TenantBOutboxTailAfterTenantBEvent.ShouldEqual(EventSequenceNumber.First);

    [Fact]
    void should_keep_tenant_a_inbox_on_first_event_after_tenant_b_event() =>
        Context.TenantAInboxTailAfterTenantBEvent.ShouldEqual(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    void should_forward_tenant_b_event_to_tenant_b_inbox() =>
        Context.TenantBInboxTailAfterTenantBEvent.ShouldEqual(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    void should_have_one_reactor_handled_event_for_tenant_a() =>
        Context.Tracker.GetHandledCount(context.TenantANamespace).ShouldEqual(1);

    [Fact]
    void should_have_one_reactor_handled_event_for_tenant_b() =>
        Context.Tracker.GetHandledCount(context.TenantBNamespace).ShouldEqual(1);

    [EventType]
    [EventStore(context.SourceEventStoreName)]
    public record AdminUserInvited(string EmailAddress);

    [DependencyInjection.IgnoreConvention]
    public class SourceOutboxForwardingReactor(IChronicleClient chronicleClient) : IReactor
    {
        public async Task On(AdminUserInvited @event, EventContext context)
        {
            var eventStore = await chronicleClient.GetEventStore(context.EventStore, context.Namespace);
            await eventStore.GetEventSequence(EventSequenceId.Outbox).Append(context.EventSourceId, @event);
        }
    }

    [DependencyInjection.IgnoreConvention]
    public class LobbyReactor(TenantForwardingTracker tracker) : IReactor
    {
        public Task On(AdminUserInvited @event, EventContext context)
        {
            tracker.Handled(context.Namespace.Value);
            return Task.CompletedTask;
        }
    }

    public class TenantForwardingTracker
    {
        readonly ConcurrentDictionary<string, int> _handledCounts = new();
        readonly ConcurrentDictionary<string, TaskCompletionSource> _waiters = new();

        public void Prepare(string @namespace) =>
            _waiters[@namespace] = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitFor(string @namespace) =>
            _waiters[@namespace].Task.WaitAsync(TimeSpanFactory.DefaultTimeout());

        public void Handled(string @namespace)
        {
            _handledCounts.AddOrUpdate(@namespace, 1, (_, current) => current + 1);

            if (_waiters.TryGetValue(@namespace, out var waiter))
            {
                waiter.TrySetResult();
            }
        }

        public int GetHandledCount(string @namespace) =>
            _handledCounts.TryGetValue(@namespace, out var handledCount) ? handledCount : 0;
    }
}
