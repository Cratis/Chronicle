// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class leaving_the_cached_writer_unbound : given.a_namespace_bound_append
{
    async Task Because() => await _unbound.AppendMany([new("source", "event")]);

    [Fact] void should_leave_the_original_writer_unchanged() => _request.ShouldNotBeNull();
    [Fact] void should_not_use_the_bound_route() => _boundBatch.ShouldBeNull();
    [Fact] void should_create_a_separate_writer() => ReferenceEquals(_unbound, _eventSequence).ShouldBeFalse();
}
