// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_creation_is_retried : given.a_client_with_an_own_store
{
    readonly InvalidOperationException _failure = new("first creation failed");
    Exception _caught;
    IEventStore _cached;

    void Establish()
    {
        _options.RegistrationRetry.MaxAttempts = 1;
        _services.EventStores.EnsureEventStore(Arg.Is<EnsureEventStoreRequest>(_ => _.Name == "foreign"))
            .Returns(_ => throw _failure, _ => CommandResult.Success(Guid.Empty));
    }

    async Task Because()
    {
        _caught = await Catch.Exception(() => _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly));
        _foreign = await _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly);
        _cached = await _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly);
    }

    [Fact] void should_surface_original_failure() => _caught.ShouldBeSame(_failure);
    [Fact] void should_retry_and_cache_successful_handle() => _cached.ShouldBeSame(_foreign);
    [Fact] void should_not_fall_back_to_inline() => ShouldNotRegisterForeignArtifacts();
    [Fact] void should_only_register_successful_catalog() => _services.EventTypes.ReceivedCalls().Count().ShouldEqual(1);
}
