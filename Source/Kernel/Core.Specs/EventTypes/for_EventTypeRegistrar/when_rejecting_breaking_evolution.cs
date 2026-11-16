// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_rejecting_breaking_evolution : given.additive_evolution_enabled
{
    [Theory]
    [InlineData("""{"type":"object","properties":{"name":{"type":"integer"},"age":{"type":"integer"}}}""")]
    [InlineData("""{"type":"object","properties":{"age":{"type":"integer"}}}""")]
    [InlineData("""{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""")]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string","format":"email"},"age":{"type":"integer"}}}""")]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string","maxLength":10},"age":{"type":"integer"}}}""")]
    public async Task should_reject_a_breaking_change_disguised_as_an_addition(string incoming)
    {
        var check = await _subject.Check("test-store", [Registration(incoming)], _storage);
        var error = await Catch.Exception(() => Register(incoming));
        check.Single().Accepted.ShouldBeFalse();
        check.Single().Classification.ShouldEqual("Breaking");
        error.ShouldBeOfExactType<EventTypeSchemaChanged>();
        await _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    }
}
