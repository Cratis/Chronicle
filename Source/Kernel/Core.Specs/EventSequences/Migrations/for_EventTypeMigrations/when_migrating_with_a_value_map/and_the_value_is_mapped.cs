// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_value_map;

/// <summary>
/// The map states that generation 1's <c>0</c> is generation 2's <c>10</c>, so upcasting an event stored at
/// generation 1 has to produce the number the newer generation gives that meaning.
/// </summary>
public class and_the_value_is_mapped : given.a_definition_with_a_value_map
{
    void Establish() => _content = new JsonObject { ["status"] = 0 };

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);

    [Fact] void should_return_both_generations() => _result.Count.ShouldEqual(2);

    [Fact] void should_translate_the_value_for_the_upcasted_generation() =>
        _expandoObjectConverter.Received().ToExpandoObject(Arg.Is<JsonObject>(_ => StatusIs(_, 10)), Arg.Any<JsonSchema>());

    [Fact] void should_leave_the_value_alone_for_the_source_generation() =>
        _expandoObjectConverter.Received().ToExpandoObject(Arg.Is<JsonObject>(_ => StatusIs(_, 0)), Arg.Any<JsonSchema>());
}
