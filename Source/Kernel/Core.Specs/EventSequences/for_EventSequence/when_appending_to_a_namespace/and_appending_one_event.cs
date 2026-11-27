// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_appending_one_event : given.a_namespace_bound_sequence
{
    AppendResult _result = null!;

    void Establish() => _stateStorage.State.ActivationIncarnation = new NamespaceIncarnationId(Guid.NewGuid());

    async Task Because() => _result = await AppendProtected();

    [Fact] void should_confirm_the_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_through_the_fence() => _written.Count.ShouldEqual(1);
    [Fact] void should_keep_the_callers_expected_identity() => _contexts.Single().Incarnation.ShouldEqual(Incarnation);
    [Fact] void should_keep_the_original_activation_identity() => _contexts.Single().ActivationIncarnation.ShouldEqual(Incarnation);
    [Fact] void should_pass_activation_lifetime_cancellation() => _contexts.Single().ActivationCancellation.CanBeCanceled.ShouldBeTrue();
    [Fact] void should_never_use_a_tokenless_append() => TokenlessAppendCount.ShouldEqual(0);
}
