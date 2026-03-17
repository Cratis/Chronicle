// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_upcasting_with_default_value : Specification
{
    DefaultValueMigration _migration;
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _migration = new DefaultValueMigration();
        _builder = new EventMigrationBuilder();
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_produce_age_property() => _result.ContainsKey("Age").ShouldBeTrue();
    [Fact] void should_have_default_value_expression() => _result["Age"].ToString().ShouldContain("$defaultValue");
    [Fact] void should_set_default_value_to_zero() => _result["Age"]["$defaultValue"].GetValue<int>().ShouldEqual(0);

    class DefaultValueMigration : EventTypeMigration<TestEventV2, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1> builder) =>
            builder.Properties(pb => pb
                .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
                .Split(t => t.LastName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second)
                .DefaultValue(t => t.Age, 0));

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV2> builder) =>
            builder.Properties(pb => pb
                .Combine(t => t.FullName, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
    }
}
