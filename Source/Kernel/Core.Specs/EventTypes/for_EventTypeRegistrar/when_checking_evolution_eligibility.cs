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
    [InlineData(true, EventTypeOwner.Client, Contracts.Events.EventTypeOwner.Client)]
    [InlineData(false, EventTypeOwner.Server, Contracts.Events.EventTypeOwner.Client)]
    [InlineData(false, EventTypeOwner.Client, Contracts.Events.EventTypeOwner.Server)]
    [InlineData(false, EventTypeOwner.Server, Contracts.Events.EventTypeOwner.Server)]
    public async Task should_reject_additions_without_permission(bool strict, EventTypeOwner storedOwner, Contracts.Events.EventTypeOwner incomingOwner)
    {
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions { StrictEventTypeSchemaEventStores = strict ? ["test-store"] : [] }));
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

    [Theory]
    [InlineData("test-store")]
    [InlineData("another-store")]
    public async Task should_accept_additions_by_default(string eventStore)
    {
        var results = await _subject.Check(eventStore, [Registration()], _storage);
        results.Single().Accepted.ShouldBeTrue();
        results.Single().Classification.ShouldEqual("Additive");
        await Register(eventStore: eventStore);
        _definition.Generations.Single().Schema.Properties.Keys.ShouldContain("age");
    }

    [Theory]
    [InlineData("Test-Store", false)]
    [InlineData("test-store", true)]
    [InlineData("another-store", true)]
    public async Task should_match_strict_event_store_names_exactly(string eventStore, bool accepted)
    {
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions { StrictEventTypeSchemaEventStores = ["Test-Store"] }));
        var results = await _subject.Check(eventStore, [Registration()], _storage);
        results.Single().Accepted.ShouldEqual(accepted);
        var error = await Catch.Exception(() => Register(eventStore: eventStore));
        (error is null).ShouldEqual(accepted);
    }

    [Fact]
    void should_bind_the_server_policy()
    {
        new ChronicleOptions().StrictEventTypeSchemaEventStores.ShouldBeEmpty();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:StrictEventTypeSchemaEventStores:0"] = "test-store"
        }).Build();
        configuration.GetSection("Cratis:Chronicle").Get<ChronicleOptions>().StrictEventTypeSchemaEventStores.ShouldContain("test-store");
    }
}
