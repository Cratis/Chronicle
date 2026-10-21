// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_upcasting_a_concept_default;

public class and_the_default_is_typed_as_the_concept : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new();

    void Because()
    {
        ((IEventTypeMigration)new ModuleAssignedV1ToV2()).Upcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_emit_the_primitive_default() => _result["Module"]!["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");
}
