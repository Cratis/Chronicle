// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_upcasting_with_split : Specification
{
    SplitMigration _migration;
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _migration = new SplitMigration();
        _builder = new EventMigrationBuilder();
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_produce_first_name_property() => _result.ContainsKey("FirstName").ShouldBeTrue();
    [Fact] void should_produce_last_name_property() => _result.ContainsKey("LastName").ShouldBeTrue();
    [Fact] void should_have_split_expression_for_first_name() => _result["FirstName"].ToString().ShouldContain("$split");
    [Fact] void should_have_split_expression_for_last_name() => _result["LastName"].ToString().ShouldContain("$split");
    [Fact] void should_reference_source_property_full_name() => _result["FirstName"].ToString().ShouldContain("FullName");
    [Fact] void should_use_space_as_separator() => _result["FirstName"]["$split"]["separator"].GetValue<string>().ShouldEqual(" ");
    [Fact] void should_extract_first_part_for_first_name() => _result["FirstName"]["$split"]["part"].GetValue<int>().ShouldEqual(0);
    [Fact] void should_extract_second_part_for_last_name() => _result["LastName"]["$split"]["part"].GetValue<int>().ShouldEqual(1);

    class SplitMigration : EventTypeMigration<TestEventV2, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1> builder) =>
            builder.Properties(pb => pb
                .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
                .Split(t => t.LastName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second));

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV2> builder) =>
            builder.Properties(pb => pb
                .Combine(t => t.FullName, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
    }
}
