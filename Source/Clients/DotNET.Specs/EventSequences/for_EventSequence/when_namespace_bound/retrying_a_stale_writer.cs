// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class retrying_a_stale_writer : given.a_namespace_bound_append
{
    void Establish() => _response.Errors = ["NamespaceIncarnationMismatch"];

    async Task Because()
    {
        await _eventSequence.AppendMany([new("source", "event")]);
        await _eventSequence.AppendMany([new("source", "event")]);
    }

    [Fact] void should_keep_the_original_identity() => _boundBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_not_lookup_a_new_identity() => services.Namespaces.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
