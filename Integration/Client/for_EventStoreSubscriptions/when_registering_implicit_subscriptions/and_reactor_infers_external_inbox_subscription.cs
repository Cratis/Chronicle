// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_registering_implicit_subscriptions.and_reactor_infers_external_inbox_subscription.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_registering_implicit_subscriptions;

[Collection(ChronicleCollection.Name)]
public class and_reactor_infers_external_inbox_subscription(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string LobbyEventStoreName = "Lobby";
        public const string AdminEventStoreName = "Admin";
        public IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> StoredSubscriptions { get; private set; } = [];

        public override IEnumerable<Type> EventTypes => [typeof(AdminUserInvited)];

        public override IEnumerable<Type> Reactors => [typeof(LobbyReactor)];

        async Task Because()
        {
            var lobbyEventStore = await ChronicleClient.GetEventStore(LobbyEventStoreName);
            await lobbyEventStore.RegisterAll();

            var subscriptionsReactor = await lobbyEventStore.Reactors.WaitForHandlerById(
                "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
                TimeSpanFactory.FromSeconds(60));

            var lobbyStorage = Services.GetRequiredService<IStorage>().GetEventStore(LobbyEventStoreName);
            var lobbySystemSequence = lobbyStorage
                .GetNamespace(Concepts.EventStoreNamespaceName.Default)
                .GetEventSequence(Concepts.EventSequences.EventSequenceId.System);
            var tailSequenceNumber = (await lobbySystemSequence.GetTailSequenceNumber()).Value;
            await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);

            StoredSubscriptions = await lobbyStorage.EventStoreSubscriptions.GetAll();
        }
    }

    [Fact]
    void should_have_persisted_one_subscription() =>
        Context.StoredSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_use_admin_event_store_as_subscription_identifier() =>
        Context.StoredSubscriptions.Single().Identifier.Value.ShouldEqual(context.AdminEventStoreName);

    [Fact]
    void should_have_admin_as_source_event_store() =>
        Context.StoredSubscriptions.Single().SourceEventStore.Value.ShouldEqual(context.AdminEventStoreName);

    [Fact]
    void should_include_the_inferred_external_event_type() =>
        Context.StoredSubscriptions.Single().EventTypes.Select(_ => _.Id.Value).ShouldContain("7f7d0845-09f0-4e74-b689-684f5488f865");

    [EventType("7f7d0845-09f0-4e74-b689-684f5488f865")]
    [EventStore(context.AdminEventStoreName)]
    record AdminUserInvited;

    [Reactor]
    class LobbyReactor : IReactor
    {
        public Task Invited(AdminUserInvited @event) => Task.CompletedTask;
    }
}
