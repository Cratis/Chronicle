// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_options_change_before_reconnecting : given.a_client_with_an_own_store
{
    async Task Establish()
    {
        _foreign = await _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly);
        _options.RegistrationMode = RegistrationMode.AtDeployment;
        ClearRegistrationRequests();
    }

    async Task Because()
    {
        await _foreign.RegisterAll();
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_keep_foreign_store_restricted() => _services.ReadModels.Received(1).RegisterMany(Arg.Is<RegisterManyRequest>(_ => _.EventStore == "own"));
    [Fact] void should_keep_own_store_inline() => _requests.Count(_ => _.EventStore == "own" && _.GlobalByEventSource.Any()).ShouldEqual(2);
    [Fact] void should_not_seed_foreign_store() => _requests.Exists(_ => _.EventStore == "foreign").ShouldBeFalse();
    [Fact] void should_register_event_types_on_both_handles() => _services.EventTypes.ReceivedCalls().Count().ShouldEqual(3);
}
