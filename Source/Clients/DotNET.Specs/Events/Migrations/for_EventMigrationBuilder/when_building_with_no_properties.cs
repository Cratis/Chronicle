// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder;

public class when_building_with_no_properties : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new EventMigrationBuilder();

    void Because() => _result = _builder.ToJson();

    [Fact] void should_return_empty_object() => _result.Count.ShouldEqual(0);
}
