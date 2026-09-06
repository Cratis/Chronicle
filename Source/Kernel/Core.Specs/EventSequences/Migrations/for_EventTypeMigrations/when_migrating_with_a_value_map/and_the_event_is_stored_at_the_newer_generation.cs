// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_value_map;

/// <summary>
/// The generations a store keeps are computed in both directions, so an event written at the newer generation has to
/// come back out at the older one carrying the number that generation gives the meaning - the map read backwards.
/// </summary>
public class and_the_event_is_stored_at_the_newer_generation : given.a_definition_with_a_value_map
{
    protected override EventTypeGeneration StoredAtGeneration => 2;

    void Establish() => _content = new JsonObject { ["status"] = 10 };

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);

    [Fact] void should_return_both_generations() => _result.Count.ShouldEqual(2);

    [Fact] void should_translate_the_value_for_the_downcasted_generation() =>
        _expandoObjectConverter.Received().ToExpandoObject(Arg.Is<JsonObject>(_ => StatusIs(_, 0)), Arg.Any<JsonSchema>());

    [Fact] void should_leave_the_value_alone_for_the_source_generation() =>
        _expandoObjectConverter.Received().ToExpandoObject(Arg.Is<JsonObject>(_ => StatusIs(_, 10)), Arg.Any<JsonSchema>());
}
