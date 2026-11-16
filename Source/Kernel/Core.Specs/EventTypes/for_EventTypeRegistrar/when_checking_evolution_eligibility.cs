// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

[PerFactSpecificationLifecycle]
public class when_checking_evolution_eligibility : given.additive_evolution_enabled
{
    [Theory]
    [InlineData(false, EventTypeOwner.Client, Contracts.Events.EventTypeOwner.Client)]
    [InlineData(true, EventTypeOwner.Server, Contracts.Events.EventTypeOwner.Client)]
    [InlineData(true, EventTypeOwner.Client, Contracts.Events.EventTypeOwner.Server)]
    public async Task should_reject_additions_without_permission(bool enabled, EventTypeOwner storedOwner, Contracts.Events.EventTypeOwner incomingOwner)
    {
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions { AdditiveEventTypeSchemaEvolutionEventStores = enabled ? ["test-store"] : [] }));
        _definition = _definition with { Owner = storedOwner };
        var registration = Registration();
        registration.Owner = incomingOwner;
        var results = await _subject.Check("test-store", [registration], _storage);
        var error = await Catch.Exception(() => _subject.Register("test-store", [registration], false, _storage, _eventTypesCacheClient, _patternCapture));
        results.Single().Accepted.ShouldBeFalse();
        results.Single().Classification.ShouldEqual("Additive");
        error.ShouldBeOfExactType<EventTypeSchemaChanged>();
        await _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    }

    [Fact]
    async Task should_not_write_during_a_check()
    {
        await _subject.Check("test-store", [Registration()], _storage);
        await _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
        await _eventTypesStorage.DidNotReceive().Register(Arg.Any<IEnumerable<EventTypeToRegister>>());
        _systemEventSequence.ReceivedCalls().ShouldBeEmpty();
        _eventTypesCacheClient.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    async Task should_reject_additions_in_another_event_store()
    {
        var results = await _subject.Check("another-store", [Registration()], _storage);
        results.Single().Accepted.ShouldBeFalse();
    }

    [Fact]
    void should_bind_the_server_policy()
    {
        new ChronicleOptions().AdditiveEventTypeSchemaEvolutionEventStores.ShouldBeEmpty();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:AdditiveEventTypeSchemaEvolutionEventStores:0"] = "test-store"
        }).Build();
        configuration.GetSection("Cratis:Chronicle").Get<ChronicleOptions>().AdditiveEventTypeSchemaEvolutionEventStores.ShouldContain("test-store");
    }
}
