// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_upcasting_and_downcasting_with_split_and_combine : Specification
{
    RoundTripMigration _migration;
    EventMigrationBuilder _upcastBuilder;
    EventMigrationBuilder _downcastBuilder;
    JsonObject _upcastResult;
    JsonObject _downcastResult;

    void Establish()
    {
        _migration = new RoundTripMigration();
        _upcastBuilder = new EventMigrationBuilder();
        _downcastBuilder = new EventMigrationBuilder();
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_upcastBuilder);
        ((IEventTypeMigration)_migration).Downcast(_downcastBuilder);
        _upcastResult = _upcastBuilder.ToJson();
        _downcastResult = _downcastBuilder.ToJson();
    }

    [Fact] void should_upcast_first_name_with_split() => _upcastResult["FirstName"].ToString().ShouldContain("$split");
    [Fact] void should_upcast_last_name_with_split() => _upcastResult["LastName"].ToString().ShouldContain("$split");
    [Fact] void should_downcast_full_name_with_combine() => _downcastResult["FullName"].ToString().ShouldContain("$combine");
    [Fact] void should_use_same_separator_in_upcast() => _upcastResult["FirstName"]["$split"]["separator"].GetValue<string>().ShouldEqual(":");
    [Fact] void should_use_same_separator_in_downcast() => _downcastResult["FullName"]["$combine"]["separator"].GetValue<string>().ShouldEqual(":");

    class RoundTripMigration : EventTypeMigration<TestEventV2, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1> builder) =>
            builder.Properties(pb => pb
                .Split(t => t.FirstName, s => s.FullName, ":", SplitPartIndex.First)
                .Split(t => t.LastName, s => s.FullName, ":", SplitPartIndex.Second));

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV2> builder) =>
            builder.Properties(pb => pb
                .Combine(t => t.FullName, ":", s => s.FirstName, s => s.LastName));
    }
}
