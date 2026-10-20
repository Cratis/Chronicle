// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_routed_batch_where_one_event_is_keyed : given.an_idempotent_append
{
    async Task Because() => await _eventSequence.AppendMany([new("a", "one") { IdempotencyKey = _key }, new("b", "two")]);

    [Fact] void should_use_the_keyed_transport() => _batchRequest.Events.Count().ShouldEqual(2);
    [Fact] void should_send_the_key_for_the_keyed_event() => _batchRequest.Events.First().IdempotencyKey.ShouldEqual("order-1");
    [Fact] void should_send_an_empty_key_for_the_unkeyed_event() => _batchRequest.Events.Last().IdempotencyKey.ShouldEqual(string.Empty);
}
