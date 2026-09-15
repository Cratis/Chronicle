// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_value_map;

/// <summary>
/// A map says which values changed meaning, so a value it stays silent about has to survive untouched - anything
/// else would make every map an exhaustive listing of an enumeration that is free to grow.
/// </summary>
public class and_the_value_is_not_in_the_map : given.a_definition_with_a_value_map
{
    void Establish() => _content = new JsonObject { ["status"] = 7 };

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);

    [Fact] void should_carry_the_value_across_unchanged() =>
        _expandoObjectConverter.Received(2).ToExpandoObject(Arg.Is<JsonObject>(_ => StatusIs(_, 7)), Arg.Any<JsonSchema>());
}
