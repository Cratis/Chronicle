// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_an_older_client_reconnects_after_a_required_nullable_addition : given.additive_evolution_enabled
{
    const string NullableAddition = """{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":["integer","null"]}}}""";
    EventTypeCheckResult _check;

    async Task Because()
    {
        await Register(NullableAddition);
        _check = (await _subject.Check("test-store", [Registration(Original)], _storage)).Single();
        await Register(Original);
    }

    [Fact] void should_accept_the_older_client() => _check.Accepted.ShouldBeTrue();
    [Fact] void should_report_subset_rather_than_tolerated() => _check.Classification.ShouldEqual("Subset");
    [Fact] void should_keep_the_new_property() => _definition.Generations.Single().Schema.Properties.Keys.ShouldContain("age");
    [Fact] void should_only_write_the_addition() => _eventTypesStorage.Received(1).TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
}
