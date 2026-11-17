// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

[PerFactSpecificationLifecycle]
public class when_rejecting_breaking_evolution : given.additive_evolution_enabled
{
    [Theory]
    [InlineData(false, """{"type":"object","properties":{"name":{"type":"integer"},"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","properties":{"name":{"type":"integer"},"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","properties":{"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","properties":{"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","properties":{"name":{"type":"string","format":"email"},"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","properties":{"name":{"type":"string","format":"email"},"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","properties":{"name":{"type":"string","maxLength":10},"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","properties":{"name":{"type":"string","maxLength":10},"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""")]
    [InlineData(true, """{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""")]
    [InlineData(false, """{"type":"object","properties":{"fullName":{"type":"string"}}}""")]
    [InlineData(true, """{"type":"object","properties":{"fullName":{"type":"string"}}}""")]
    [InlineData(false, """{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer","default":0}}}""")]
    [InlineData(true, """{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer","default":0}}}""")]
    public async Task should_reject_a_breaking_change_disguised_as_an_addition(bool strict, string incoming)
    {
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions { StrictEventTypeSchemaEventStores = strict ? ["test-store"] : [] }));
        var check = await _subject.Check("test-store", [Registration(incoming)], _storage);
        var error = await Catch.Exception(() => Register(incoming));
        check.Single().Accepted.ShouldBeFalse();
        check.Single().Classification.ShouldEqual("Breaking");
        error.ShouldBeOfExactType<EventTypeSchemaChanged>();
        await _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_removing_a_required_property(bool strict)
    {
        _definition = StoredEventType("some-event", (1, """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""));
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions { StrictEventTypeSchemaEventStores = strict ? ["test-store"] : [] }));
        const string incoming = """{"type":"object","properties":{}}""";
        var results = await _subject.Check("test-store", [Registration(incoming)], _storage);
        results.Single().Accepted.ShouldBeFalse();
        (await Catch.Exception(() => Register(incoming))).ShouldBeOfExactType<EventTypeSchemaChanged>();
        await _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    }
}
