// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class with_event_types_only : given.a_client_with_an_own_store
{
    async Task Because() => _foreign = await _client.GetEventStore("foreign", "tenant", RegistrationMode.EventTypesOnly);

    [Fact] void should_register_own_read_models_normally() => _ownReadModelRegistrations.ShouldEqual(1);
    [Fact] void should_seed_own_store_normally() => _requests.Exists(_ => _.EventStore == "own").ShouldBeTrue();
    [Fact] void should_ensure_foreign_store() => _services.EventStores.Received(1).EnsureEventStore(Arg.Is<EnsureEventStoreRequest>(_ => _.Name == "foreign"));
    [Fact] void should_register_foreign_event_catalog() => _services.EventTypes.Received(1).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(_ => _.EventStore == "foreign"));
    [Fact] void should_not_register_foreign_artifacts() => ShouldNotRegisterForeignArtifacts();
    [Fact] void should_share_the_connection() => _foreign.Connection.ShouldBeSame(_own.Connection);
    [Fact] void should_not_change_global_mode() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
}
