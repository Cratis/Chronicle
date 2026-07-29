// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.given;

public class an_event_types_storage : Specification
{
    protected EventTypesStorage _storage;

    void Establish() => _storage = new();
}
