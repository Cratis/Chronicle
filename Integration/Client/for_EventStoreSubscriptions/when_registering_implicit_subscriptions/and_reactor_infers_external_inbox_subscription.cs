// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_registering_implicit_subscriptions.and_reactor_infers_external_inbox_subscription.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_registering_implicit_subscriptions;

[Collection(ChronicleCollection.Name)]
public class and_reactor_infers_external_inbox_subscription(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string LobbyEventStoreName = "Lobby";
        public const string AdminEventStoreName = "Admin";
        public IEnumerable<EventStoreSubscriptionDefinition> StoredSubscriptions { get; private set; } = [];

        public override IEnumerable<Type> EventTypes => [typeof(AdminUserInvited)];

        public override IEnumerable<Type> Reactors => [typeof(LobbyReactor)];

        async Task Because()
        {
            var lobbyEventStore = await ChronicleClient.GetEventStore(LobbyEventStoreName);
            await lobbyEventStore.RegisterAll();

            // Poll until the implicit subscription has been persisted. Checking the system
            // log tail first is unreliable because the tail may be Unavailable (no events yet),
            // which would cause WaitTillReachesEventSequenceNumber to return immediately.
            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (true)
            {
                StoredSubscriptions = await lobbyEventStore.Subscriptions.GetAll();
                if (StoredSubscriptions.Any())
                {
                    break;
                }

                await Task.Delay(100, cts.Token);
            }
        }
    }

    [Fact]
    void should_have_persisted_one_subscription() =>
        Context.StoredSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_use_admin_event_store_as_subscription_identifier() =>
        Context.StoredSubscriptions.Single().Id.Value.ShouldEqual(context.AdminEventStoreName);

    [Fact]
    void should_have_admin_as_source_event_store() =>
        Context.StoredSubscriptions.Single().SourceEventStore.ShouldEqual(context.AdminEventStoreName);

    [Fact]
    void should_include_the_inferred_external_event_type() =>
        Context.StoredSubscriptions.Single().EventTypes.Select(_ => _.Value).ShouldContain("7f7d0845-09f0-4e74-b689-684f5488f865");

    [EventType("7f7d0845-09f0-4e74-b689-684f5488f865")]
    [EventStore(context.AdminEventStoreName)]
    record AdminUserInvited;

    [Reactor]
    class LobbyReactor : IReactor
    {
        public Task Invited(AdminUserInvited @event) => Task.CompletedTask;
    }
}
