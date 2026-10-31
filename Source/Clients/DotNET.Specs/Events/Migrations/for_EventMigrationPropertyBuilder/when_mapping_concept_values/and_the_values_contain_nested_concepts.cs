// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_mapping_concept_values;

public class and_the_values_contain_nested_concepts : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.MapValues("Defaults", "OldDefaults", [new ValueMapping(new ModuleDefaults(new("local")), new ModuleDefaults(new("global")))]);

    [Fact] void should_unwrap_the_nested_source_concept() => _builder.Properties["Defaults"]["$mapValues"]!["mappings"]![0]!["from"]!["Module"]!.ToJsonString().ShouldEqual("\"local\"");
    [Fact] void should_unwrap_the_nested_target_concept() => _builder.Properties["Defaults"]["$mapValues"]!["mappings"]![0]!["to"]!["Module"]!.ToJsonString().ShouldEqual("\"global\"");
}
