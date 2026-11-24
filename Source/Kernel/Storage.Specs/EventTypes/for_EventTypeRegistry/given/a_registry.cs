// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.given;

public class a_registry : Specification
{
    protected static readonly EventTypeId _eventTypeId = "some-event";
    protected static readonly EventTypeId _otherEventTypeId = "some-other-event";

    protected PlainEventTypesStorage _storage;
    protected ManualTimeProvider _time;
    protected EventTypeRegistry _registry;

    void Establish()
    {
        _storage = new();
        _storage.Seed(_eventTypeId, (1, "first"), (2, "second"));
        _storage.Seed(_otherEventTypeId, (1, "other"));
        _time = new();
        _registry = new(_storage, _time);
    }
}
