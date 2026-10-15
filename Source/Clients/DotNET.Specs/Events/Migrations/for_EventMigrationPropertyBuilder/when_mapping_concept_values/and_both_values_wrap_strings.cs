// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_mapping_concept_values;

public class and_both_values_wrap_strings : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.MapValues("Module", "Module", [new ValueMapping(new ModuleKey("legacy"), new ModuleKey("global"))]);

    [Fact] void should_emit_the_primitive_source() => _builder.Properties["Module"]["$mapValues"]!["mappings"]![0]!["from"]!.ToJsonString().ShouldEqual("\"legacy\"");
    [Fact] void should_emit_the_primitive_target() => _builder.Properties["Module"]["$mapValues"]!["mappings"]![0]!["to"]!.ToJsonString().ShouldEqual("\"global\"");
}
