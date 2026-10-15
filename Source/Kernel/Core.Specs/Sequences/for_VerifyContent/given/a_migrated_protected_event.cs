// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_migrated_protected_event : a_protected_event
{
    async Task Establish()
    {
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        var schema = await eventTypes.GetFor("event", 1U);
        eventTypes.HasFor("event", 2U).Returns(true);
        eventTypes.GetFor("event", 2U).Returns(schema);
        eventTypes.Configure().GetDefinition("event").Returns(new EventTypeDefinition(
            "event",
            EventTypeOwner.Client,
            false,
            [new(1, schema.Schema), new(2, schema.Schema)],
            [new(1, 2, [], new JsonObject(), new JsonObject())]));
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = _stored.GenerationalContent[1], [2] = _stored.GenerationalContent[1] } };
    }
}
