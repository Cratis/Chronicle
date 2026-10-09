// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_visibility_and_origin : given.an_event_types_storage
{
    IEnumerable<EventTypeSchema> _schemas;

    async Task Establish()
    {
        await _storage.Register(new EventType("public-event", EventTypeGeneration.First), new JsonSchema(), EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Public, "owning-store");
        await _storage.Register(new EventType("private-event", EventTypeGeneration.First), new JsonSchema(), EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Private);
        await _storage.Register(new EventType("old-client-event", EventTypeGeneration.First), new JsonSchema());

        // A client that predates visibility re-registers - it said nothing, so it must not undo what is declared.
        await _storage.Register(new EventType("public-event", EventTypeGeneration.First), new JsonSchema(), EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Unspecified, "owning-store");
    }

    async Task Because() => _schemas = await _storage.GetLatestForAllEventTypes();

    [Fact] void should_keep_the_public_visibility() => For("public-event").Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_keep_the_origin() => For("public-event").Origin.ShouldEqual("owning-store");
    [Fact] void should_keep_the_private_visibility() => For("private-event").Visibility.ShouldEqual(EventTypeVisibility.Private);
    [Fact] void should_read_a_registration_without_visibility_as_unspecified() => For("old-client-event").Visibility.ShouldEqual(EventTypeVisibility.Unspecified);

    EventTypeSchema For(string id) => _schemas.Single(_ => _.Type.Id == id);
}
