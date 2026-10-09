// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage.when_getting_covering;

public class and_no_mask_matches : given.a_closed_streams_storage
{
    IEnumerable<ClosedStream> _result;

    async Task Because() => _result = await _storage.GetCovering(new(EventSourceId: "source-a"), [ClosedStreamDimensions.EventStreamId]);

    [Fact] void should_find_no_closures() => _result.ShouldBeEmpty();
}
