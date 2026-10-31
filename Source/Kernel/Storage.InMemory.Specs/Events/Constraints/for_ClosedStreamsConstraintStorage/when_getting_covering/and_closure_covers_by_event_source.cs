// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage.when_getting_covering;

public class and_closure_covers_by_event_source : given.a_closed_streams_storage
{
    IEnumerable<ClosedStream> _result;

    async Task Because() => _result = await _storage.GetCovering(new(EventSourceId: "source-a", EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default), [ClosedStreamDimensions.EventSourceId]);

    [Fact] void should_find_the_covering_closure() => _result.ShouldContainOnly(_closure);
}
