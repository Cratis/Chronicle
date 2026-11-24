// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class appending_a_single_event : given.a_namespace_bound_append
{
    async Task Because() => await _eventSequence.AppendWithNamedTags("source", "event", [new("category", "receipt")]);

    [Fact] void should_send_the_captured_identity() => _boundSingle.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_send_the_canonical_namespace() => _boundSingle.Namespace.ShouldEqual(_binding.Namespace.Value);
    [Fact] void should_preserve_named_tags() => _boundSingle.NamedTags.Single().Value.ShouldEqual("receipt");
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
