// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using MongoDB.Driver;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

public class when_preparing_repeated_ids_in_a_batch : given.a_mocked_event_types_storage
{
    async Task Because()
    {
        var first = new EventTypeDefinition(_eventTypeId, EventTypeOwner.Client, false, [new(1, new JsonSchema()), new(2, new JsonSchema())], [new(1, 2, [], new JsonObject(), new JsonObject())]);
        var second = first with { Generations = [new(3, new JsonSchema())], Migrations = [new(2, 3, [], new JsonObject(), new JsonObject())] };
        await _storage.Register([new(first, EventTypeSource.Code), new(second, EventTypeSource.Code)]);
    }

    [Fact] async Task should_emit_one_final_write_for_the_id() => await _collection.Received(1).BulkWriteAsync(
        Arg.Is<IEnumerable<WriteModel<MongoEventType>>>(_ => _.Count() == 1),
        Arg.Is<BulkWriteOptions>(_ => !_.IsOrdered),
        default);
}
