// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Seeding;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_namespace_stores.given;

public class a_client_with_seeds : for_EventStore.when_preparing_for_registration.given.an_explicit_store
{
    protected ChronicleClient _client;
    protected readonly List<SeedEventsRequest> _requests = [];
    readonly object _requestsLock = new();

    async Task Establish()
    {
        await _connection.Lifecycle.Connected();
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("plain"));
        _artifacts.EventSeeders.Returns([typeof(Seeds)]);
        _services.Seeding.SeedEvents(Arg.Any<SeedEventsRequest>()).Returns(call =>
        {
            lock (_requestsLock) _requests.Add(call.Arg<SeedEventsRequest>());
            return CommandResult.Success(Guid.NewGuid());
        });
        _client = new(_connection, _options, _artifacts, _provider);
    }

    void Destroy() => _client.Dispose();

    public class Seeds : ICanSeedEvents
    {
        public void Seed(IEventSeedingBuilder builder)
        {
            builder.For("global", [new PlainEvent("global")]);
            builder.ForNamespace(EventStoreNamespaceName.Default).For("local", [new PlainEvent("local")]);
        }
    }
}
