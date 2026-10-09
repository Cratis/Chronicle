// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage.given;

public class a_closed_streams_storage : Specification
{
    protected ClosedStreamsConstraintStorage _storage;
    protected ClosedStream _closure;

    async Task Establish()
    {
        _storage = new();
        _closure = new(new(EventSourceId: "source-a"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null);
        await _storage.Close(_closure);
    }
}
