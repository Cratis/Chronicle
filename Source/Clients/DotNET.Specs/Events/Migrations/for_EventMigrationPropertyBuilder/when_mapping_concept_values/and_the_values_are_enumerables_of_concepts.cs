// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_mapping_concept_values;

public class and_the_values_are_enumerables_of_concepts : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.MapValues("Modules", "OldModules", [new ValueMapping(new List<ModuleKey> { new("local") }, new List<ModuleKey> { new("global") })]);

    [Fact] void should_emit_the_source_as_an_array_of_values() => _builder.Properties["Modules"]["$mapValues"]!["mappings"]![0]!["from"]!.ToJsonString().ShouldEqual("[\"local\"]");
    [Fact] void should_emit_the_target_as_an_array_of_values() => _builder.Properties["Modules"]["$mapValues"]!["mappings"]![0]!["to"]!.ToJsonString().ShouldEqual("[\"global\"]");
}
