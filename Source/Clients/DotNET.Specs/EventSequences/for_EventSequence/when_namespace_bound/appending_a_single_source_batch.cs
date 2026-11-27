// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class appending_a_single_source_batch : given.a_namespace_bound_append
{
    async Task Because() => await _eventSequence.AppendMany("source", ["first", "second"]);

    [Fact] void should_send_the_captured_identity() => _boundBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_preserve_the_order() => _boundBatch.Events.Select(@event => @event.EventSourceId).ShouldEqual(["source", "source"]);
    [Fact] void should_append_every_event() => _boundBatch.Events.Count().ShouldEqual(2);
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
