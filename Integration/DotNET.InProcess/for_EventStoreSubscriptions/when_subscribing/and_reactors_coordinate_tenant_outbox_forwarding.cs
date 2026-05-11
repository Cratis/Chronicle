// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_reactors_coordinate_tenant_outbox_forwarding.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_reactors_coordinate_tenant_outbox_forwarding(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string SourceEventStoreName = "tenant-forwarding-admin";
        public const string TargetEventStoreName = "tenant-forwarding-lobby";
        public const string SubscriptionId = "tenant-forwarding-admin-subscription";
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
            var targetEventStore = await ChronicleClient.GetEventStore(TargetEventStoreName);
            var sourceTenantA = await ChronicleClient.GetEventStore(SourceEventStoreName, TenantANamespace);
            var sourceTenantB = await ChronicleClient.GetEventStore(SourceEventStoreName, TenantBNamespace);
            var targetTenantA = await ChronicleClient.GetEventStore(TargetEventStoreName, TenantANamespace);
            var targetTenantB = await ChronicleClient.GetEventStore(TargetEventStoreName, TenantBNamespace);

            await Task.WhenAll(
                sourceTenantA.DiscoverAll(),
                sourceTenantB.DiscoverAll(),
                targetTenantA.DiscoverAll(),
                targetTenantB.DiscoverAll());

            await Task.WhenAll(
                sourceEventStore.EventTypes.Register(),
                targetEventStore.EventTypes.Register());

            var connectionServices = (targetTenantA.Connection as IChronicleServicesAccessor)!;
            await Task.WhenAll(
                connectionServices.Services.Namespaces.EnsureNamespace(new EnsureNamespaceRequest { EventStore = SourceEventStoreName, Namespace = TenantANamespace }),
                connectionServices.Services.Namespaces.EnsureNamespace(new EnsureNamespaceRequest { EventStore = SourceEventStoreName, Namespace = TenantBNamespace }),
                connectionServices.Services.Namespaces.EnsureNamespace(new EnsureNamespaceRequest { EventStore = TargetEventStoreName, Namespace = TenantANamespace }),
                connectionServices.Services.Namespaces.EnsureNamespace(new EnsureNamespaceRequest { EventStore = TargetEventStoreName, Namespace = TenantBNamespace }));

            var targetReactorA = await targetTenantA.Reactors.Register<LobbyReactor>();
            var targetReactorB = await targetTenantB.Reactors.Register<LobbyReactor>();

            await targetTenantA.Subscriptions.Subscribe(
                new EventStoreSubscriptionId(SubscriptionId),
                SourceEventStoreName,
                builder => builder.WithEventType<AdminUserInvited>());

            var subscriptionsManager = Services.GetRequiredService<IGrainFactory>().GetGrain<IEventStoreSubscriptionsManager>(TargetEventStoreName);
            await subscriptionsManager.WaitUntilSubscribed(new Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionId(SubscriptionId), TimeSpanFactory.FromSeconds(60));

            await targetReactorA.WaitTillSubscribed();
            await targetReactorB.WaitTillSubscribed();
            await targetReactorA.WaitTillActive();
            await targetReactorB.WaitTillActive();

            Tracker.Prepare(TenantANamespace);
            await sourceTenantA.GetEventSequence(EventSequenceId.Outbox).Append("tenant-a-user", new AdminUserInvited("tenant-a@chronicle.dev"));
            await WaitForInboxTailSequenceNumber(TenantANamespace, Concepts.Events.EventSequenceNumber.First);
            await Tracker.WaitFor(TenantANamespace);

            TenantAOutboxTailAfterTenantAEvent = await GetOutboxTailSequenceNumber(sourceTenantA);
            TenantAInboxTailAfterTenantAEvent = await GetInboxTailSequenceNumber(TenantANamespace);
            TenantBInboxTailAfterTenantAEvent = await GetInboxTailSequenceNumber(TenantBNamespace);

            Tracker.Prepare(TenantBNamespace);
            await sourceTenantB.GetEventSequence(EventSequenceId.Outbox).Append("tenant-b-user", new AdminUserInvited("tenant-b@chronicle.dev"));
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

        /// <summary>
        /// Waits until the inbox reaches at least the expected sequence number.
        /// </summary>
        /// <param name="targetNamespace">Namespace to inspect.</param>
        /// <param name="expected">Minimum expected sequence number.</param>
        /// <exception cref="TimeoutException">Thrown when the inbox does not reach the expected sequence number before timeout.</exception>
        /// <remarks>
        /// Uses a greater-than-or-equal comparison to avoid timing races where the inbox advances past
        /// the exact sequence number before the polling loop observes it.
        /// </remarks>
        async Task WaitForInboxTailSequenceNumber(string targetNamespace, Concepts.Events.EventSequenceNumber expected)
        {
            var timeout = DateTime.UtcNow.Add(TimeSpanFactory.DefaultTimeout());

            while (DateTime.UtcNow < timeout)
            {
                var tailSequenceNumber = await GetInboxTailSequenceNumber(targetNamespace);
                if (tailSequenceNumber.Value >= expected.Value)
                {
                    return;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException($"Inbox for namespace '{targetNamespace}' did not reach sequence number {expected.Value}.");
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
    public record AdminUserInvited(string EmailAddress);

    [DependencyInjection.IgnoreConvention]
    [EventStore(context.SourceEventStoreName)]
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
