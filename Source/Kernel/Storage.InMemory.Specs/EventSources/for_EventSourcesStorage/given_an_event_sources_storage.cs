// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.InMemory.EventSources.for_EventSourcesStorage;

public class given_an_event_sources_storage : Specification
{
    protected EventSourcesStorage _storage;

    void Establish() => _storage = new();

    protected static EventSourceDefinition Definition(string name, string description = "", params string[] streams) => new(
        name,
        description,
        EventSourceOwner.Client,
        ConcurrencyDimensions.EventSourceId,
        [.. streams.Select(stream => new EventStreamDefinition(stream, string.Empty, ConcurrencyDimensions.None))]);
}
