// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ClosedStreamsConstraintStorage.when_getting_covering;

[Collection(MongoDBCollection.Name)]
public class and_closure_covers_by_event_source(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    ClosedStreamsConstraintStorage _storage;
    ClosedStream _closure;
    IEnumerable<ClosedStream> _result;

    async Task Establish()
    {
        _storage = new(_database, EventSequenceId.Log);
        _closure = new(new(EventSourceId: "source-a"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null);
        await _storage.Close(_closure);
        await _storage.Close(new(new(EventSourceId: "source-b"), "constraint", EventSequenceNumber.First, null));
    }

    async Task Because() => _result = await _storage.GetCovering(new(EventSourceId: "source-a", EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default), [ClosedStreamDimensions.EventSourceId]);

    [Fact] void should_find_only_the_matching_source() => _result.ShouldContainOnly(_closure);
}
