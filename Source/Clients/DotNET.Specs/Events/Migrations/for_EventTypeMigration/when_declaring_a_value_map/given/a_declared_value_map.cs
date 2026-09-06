// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.when_declaring_a_value_map.given;

/// <summary>
/// A migration whose only statement about the two generations is a value map, so what ends up in the upcast and the
/// downcast is entirely what the map produced.
/// </summary>
public class a_declared_value_map : Specification
{
    protected JsonObject _upcast;
    protected JsonObject _downcast;

    void Establish()
    {
        var migration = new ValueMapMigration();
        var upcastBuilder = new EventMigrationBuilder();
        var downcastBuilder = new EventMigrationBuilder();

        ((IEventTypeMigration)migration).Upcast(upcastBuilder);
        ((IEventTypeMigration)migration).Downcast(downcastBuilder);

        _upcast = upcastBuilder.ToJson();
        _downcast = downcastBuilder.ToJson();
    }

    protected static JsonNode Mapping(JsonObject direction, int index) =>
        direction["Status"]["$mapValues"]["mappings"][index]!;

    class ValueMapMigration : EventTypeMigration<ValueMapTestEventV2, ValueMapTestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder)
        {
        }

        public override void Downcast(IEventMigrationBuilder<ValueMapTestEventV1, ValueMapTestEventV2> builder)
        {
        }

        public override void MapValues(IEventValueMapBuilder<ValueMapTestEventV2, ValueMapTestEventV1> builder) =>
            builder.For(current => current.Status, previous => previous.Status, map => map
                .Map(ValueMapTestStatusV1.Unknown, ValueMapTestStatus.Unspecified)
                .Map(ValueMapTestStatusV1.Verified, ValueMapTestStatus.Confirmed)
                .Map(ValueMapTestStatusV1.Revoked, ValueMapTestStatus.Withdrawn));
    }
}
