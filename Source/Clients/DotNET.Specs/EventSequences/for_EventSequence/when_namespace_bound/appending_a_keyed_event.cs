// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class appending_a_keyed_event : given.a_namespace_bound_append
{
    async Task Because() => await _eventSequence.AppendOnce("source", "event", new("same-key"));

    [Fact] void should_send_the_captured_identity() => _boundOnce.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_preserve_the_key() => _boundOnce.IdempotencyKey.ShouldEqual("same-key");
    [Fact] void should_not_require_named_tags() => _boundOnce.NamedTags.ShouldBeEmpty();
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
