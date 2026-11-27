// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class appending_a_mixed_key_batch : given.a_namespace_bound_append
{
    async Task Because() => await _eventSequence.AppendMany([
        new("first", "event") { IdempotencyKey = new("key"), NamedTags = [new("category", "receipt")] },
        new("second", "event")]);

    [Fact] void should_send_the_captured_identity() => _boundKeyedBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_preserve_keys_and_order() => _boundKeyedBatch.Events.Select(@event => @event.IdempotencyKey).ShouldEqual(["key", string.Empty]);
    [Fact] void should_preserve_per_event_named_tags() => _boundKeyedBatch.Events.First().Event.NamedTags.Single().Value.ShouldEqual("receipt");
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
