// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_object_values_are_mapped : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());
    void Because() => _builder.MapValues("mapped_Module", "mapped_Module", [new ValueMapping(new ModuleDefaults(new("legacy")), new ModuleDefaults(new("global")))]);

    [Fact] void should_prefix_members_of_the_source_value() => _builder.Properties["mapped_Module"]["$mapValues"]!["mappings"]![0]!["from"]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"legacy\"");
    [Fact] void should_prefix_members_of_the_target_value() => _builder.Properties["mapped_Module"]["$mapValues"]!["mappings"]![0]!["to"]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"global\"");
}
