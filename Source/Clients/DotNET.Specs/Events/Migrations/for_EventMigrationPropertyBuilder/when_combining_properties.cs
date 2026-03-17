// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_combining_properties : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.Combine("FullName", " ", "FirstName", "LastName");

    [Fact] void should_have_property_keyed_by_target_name() => _builder.Properties.ContainsKey("FullName").ShouldBeTrue();

    [Fact] void should_have_combine_expression() => _builder.Properties["FullName"].ToString().ShouldContain("$combine");

    [Fact] void should_only_add_one_property() => _builder.Properties.Count.ShouldEqual(1);
}
