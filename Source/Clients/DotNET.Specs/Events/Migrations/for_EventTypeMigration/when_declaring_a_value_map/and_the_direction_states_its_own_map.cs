// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map;

/// <summary>
/// The derived map is a default, not a law - a direction that states its own answer keeps it, which is the way out
/// when the inverse the map derives is not the one the domain wants.
/// </summary>
public class and_the_direction_states_its_own_map : Specification
{
    JsonObject _downcast;

    void Establish()
    {
        var builder = new EventMigrationBuilder();
        ((IEventTypeMigration)new OverridingMigration()).Downcast(builder);
        _downcast = builder.ToJson();
    }

    [Fact] void should_keep_the_map_the_direction_stated() =>
        _downcast["Status"]["$mapValues"]["mappings"][0]["to"].GetValue<int>().ShouldEqual((int)ValueMapTestStatusV1.Revoked);

    class OverridingMigration : EventTypeMigration<ValueMapTestEventV2, ValueMapTestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder)
        {
        }

        public override void Downcast(IEventMigrationBuilder<ValueMapTestEventV1, ValueMapTestEventV2> builder) =>
            builder.Properties(properties => properties
                .MapValues(previous => previous.Status, current => current.Status, map => map
                    .Map(ValueMapTestStatus.Confirmed, ValueMapTestStatusV1.Revoked)));

        public override void MapValues(IEventValueMapBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder) =>
            builder.For(current => current.Status, previous => previous.Status, map => map
                .Map(ValueMapTestStatusV1.Verified, ValueMapTestStatus.Confirmed));
    }
}
