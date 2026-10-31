// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage;

public class when_closing_twice : given.a_closed_streams_storage
{
    async Task Because() => await _storage.Close(_closure with { Scope = _closure.Scope with { EventSourceType = EventSourceType.Unspecified } });

    [Fact] async Task should_upsert_the_normalized_scope() => (await _storage.GetAll()).ShouldContainOnly(_closure);
}
