// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_setting_default_value_with_target_property : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.DefaultValue("status", "active");

    [Fact] void should_have_property_keyed_by_property_name() => _builder.Properties.ContainsKey("status").ShouldBeTrue();
    [Fact] void should_have_default_value_expression() => _builder.Properties["status"].ToString().ShouldContain("$defaultValue");
    [Fact] void should_only_add_one_property() => _builder.Properties.Count.ShouldEqual(1);
}
