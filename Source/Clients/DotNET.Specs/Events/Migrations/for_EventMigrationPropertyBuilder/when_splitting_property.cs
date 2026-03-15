// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_splitting_property : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.Split("FirstName", "FullName", " ", 0);

    [Fact] void should_have_property_keyed_by_target_name() => _builder.Properties.ContainsKey("FirstName").ShouldBeTrue();

    [Fact] void should_have_split_expression() => _builder.Properties["FirstName"].ToString().ShouldContain("$split");

    [Fact] void should_only_add_one_property() => _builder.Properties.Count.ShouldEqual(1);
}
