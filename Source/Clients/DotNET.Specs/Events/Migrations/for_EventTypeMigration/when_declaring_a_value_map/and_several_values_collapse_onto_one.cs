// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map;

/// <summary>
/// Two members collapsing onto one have no single inverse, so the derived downcast picks the first pair declared for
/// that member rather than emitting two mappings that would fight over the same value. A migration that needs the
/// other answer states the reverse map itself.
/// </summary>
public class and_several_values_collapse_onto_one : Specification
{
    JsonObject _downcast;

    void Establish()
    {
        var builder = new EventMigrationBuilder();
        ((IEventTypeMigration)new CollapsingMigration()).Downcast(builder);
        _downcast = builder.ToJson();
    }

    [Fact] void should_produce_one_mapping_for_the_shared_member() =>
        _downcast["Status"]["$mapValues"]["mappings"].AsArray().Count.ShouldEqual(1);

    [Fact] void should_map_back_to_the_first_member_declared_for_it() =>
        _downcast["Status"]["$mapValues"]["mappings"][0]["to"].GetValue<int>().ShouldEqual((int)ValueMapTestStatusV1.Verified);

    class CollapsingMigration : EventTypeMigration<ValueMapTestEventV2, ValueMapTestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder)
        {
        }

        public override void Downcast(IEventMigrationBuilder<ValueMapTestEventV1, ValueMapTestEventV2> builder)
        {
        }

        public override void MapValues(IEventValueMapBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder) =>
            builder.For(current => current.Status, previous => previous.Status, map => map
                .Map(ValueMapTestStatusV1.Verified, ValueMapTestStatus.Confirmed)
                .Map(ValueMapTestStatusV1.Revoked, ValueMapTestStatus.Confirmed));
    }
}
