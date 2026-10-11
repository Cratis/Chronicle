// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_setting_a_concept_default_value;

public class and_the_concept_is_nested_in_an_object : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new();
    void Because() => _builder.DefaultValue("Defaults", new ModuleDefaults(new("global")));

    [Fact] void should_unwrap_the_nested_concept() => _builder.Properties["Defaults"]["$defaultValue"]!["Module"]!.ToJsonString().ShouldEqual("\"global\"");
}
