// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_setting_a_concept_default_value;

public class and_the_concept_wraps_a_string : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.DefaultValue("Module", new ModuleKey("global"));

    [Fact] void should_emit_the_underlying_primitive() => _builder.Properties["Module"]["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");
}
