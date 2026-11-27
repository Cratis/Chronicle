// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_an_empty_conjunction_batch_is_fenced : given.a_namespace_bound_receipt_sequence
{
    AppendManyResult _result = null!;

    async Task Because() => _result = await _eventSequence.AppendManyWithConjunctionsToNamespace(Incarnation, [], _correlationId, [], Identity.System, Conjunctions);

    [Fact] void should_confirm_the_empty_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_still_acquire_the_fence() => _fenced.Received(1).AppendMany(Arg.Any<NamespaceCommitContext>(), Arg.Is<IEnumerable<EventToAppendToStorage>>(events => !events.Any()));
    [Fact] void should_not_use_tokenless_appends() => TokenlessAppendCount.ShouldEqual(0);
}
