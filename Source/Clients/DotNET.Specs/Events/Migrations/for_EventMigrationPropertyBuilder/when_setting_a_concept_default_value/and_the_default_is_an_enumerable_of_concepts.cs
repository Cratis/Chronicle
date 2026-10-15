// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_setting_a_concept_default_value;

public class and_the_default_is_an_enumerable_of_concepts : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.DefaultValue("Modules", new List<ModuleKey> { new("global"), new("local") });

    [Fact] void should_emit_an_array_of_underlying_values() => _builder.Properties["Modules"]["$defaultValue"]!.ToJsonString().ShouldEqual("[\"global\",\"local\"]");
}
