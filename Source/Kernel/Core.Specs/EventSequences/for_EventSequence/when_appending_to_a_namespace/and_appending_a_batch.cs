// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_appending_a_batch : given.a_namespace_bound_sequence
{
    AppendManyResult _result = null!;

    async Task Because() => _result = await AppendBatchProtected();

    [Fact] void should_confirm_the_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_every_event_under_one_fence() => _written.Count.ShouldEqual(_events.Count);
    [Fact] void should_bind_the_entire_batch_to_the_expected_identity() => _contexts.Single().Incarnation.ShouldEqual(Incarnation);
    [Fact] void should_not_use_tokenless_batch_storage() => TokenlessAppendCount.ShouldEqual(0);
}
