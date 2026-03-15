// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_renaming_property : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.RenamedFrom("NewName", "OldName");

    [Fact] void should_have_property_keyed_by_target_name() => _builder.Properties.ContainsKey("NewName").ShouldBeTrue();

    [Fact] void should_have_rename_expression() => _builder.Properties["NewName"].ToString().ShouldContain("$rename");

    [Fact] void should_only_add_one_property() => _builder.Properties.Count.ShouldEqual(1);
}
