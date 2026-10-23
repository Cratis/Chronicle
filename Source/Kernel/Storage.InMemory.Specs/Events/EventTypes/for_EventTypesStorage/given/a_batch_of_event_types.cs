// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.given;

public class a_batch_of_event_types : an_event_types_storage
{
    protected EventTypeToRegister _simple;
    protected EventTypeToRegister _multiGeneration;
    protected List<EventTypeToRegister> _batch;
    protected List<IEnumerable<EventTypeSchema>> _published;

    void Establish()
    {
        _simple = new(
            new EventTypeDefinition("simple-event", EventTypeOwner.Client, false, [new(1, new JsonSchema { Description = "simple" })], []),
            EventTypeSource.Code,
            EventTypeVisibility.Public,
            "an-origin");
        _multiGeneration = new(
            new EventTypeDefinition(
                "migrated-event",
                EventTypeOwner.Client,
                false,
                [new(1, new JsonSchema()), new(2, new JsonSchema { Description = "second" })],
                [new EventTypeMigrationDefinition(1, 2, [], new JsonObject { ["value"] = "up" }, new JsonObject())]),
            EventTypeSource.Code);
        _batch = [_simple, _multiGeneration];
        _published = [];
    }
}
