// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_explicit_builtin_batch_routes : given.an_acknowledged_append
{
    async Task Because() => await _eventSequence.AppendMany(_source, ["first", "second"], EventStreamType.All, EventStreamId.Default, EventSourceType.Default);

    [Fact] void should_use_the_rich_rpc() => _batchRequest.ShouldNotBeNull();
    [Fact] void should_preserve_explicit_source_types() => _batchRequest.Events.Select(_ => _.EventSourceType).ShouldEqual(["Default", "Default"]);
    [Fact] void should_preserve_explicit_stream_types() => _batchRequest.Events.Select(_ => _.EventStreamType).ShouldEqual(["All", "All"]);
    [Fact] void should_preserve_explicit_stream_ids() => _batchRequest.Events.Select(_ => _.EventStreamId).ShouldEqual(["Default", "Default"]);
}
