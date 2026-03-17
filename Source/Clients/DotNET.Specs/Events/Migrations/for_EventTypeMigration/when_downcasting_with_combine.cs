// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_downcasting_with_combine : Specification
{
    CombineMigration _migration;
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _migration = new CombineMigration();
        _builder = new EventMigrationBuilder();
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Downcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_produce_full_name_property() => _result.ContainsKey("FullName").ShouldBeTrue();
    [Fact] void should_have_combine_expression() => _result["FullName"]!.ToString().ShouldContain("$combine");
    [Fact] void should_include_first_name_as_source() => _result["FullName"]!["$combine"]!["sources"]!.AsArray().Any(s => s!.GetValue<string>() == "FirstName").ShouldBeTrue();
    [Fact] void should_include_last_name_as_source() => _result["FullName"]!["$combine"]!["sources"]!.AsArray().Any(s => s!.GetValue<string>() == "LastName").ShouldBeTrue();
    [Fact] void should_use_space_as_separator() => _result["FullName"]!["$combine"]!["separator"]!.GetValue<string>().ShouldEqual(" ");

    class CombineMigration : EventTypeMigration<TestEventV2, TestEventV1>
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
