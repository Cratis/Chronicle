// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_the_commit_outcome_is_unknown : given.a_namespace_bound_sequence
{
    readonly NamespaceCommitOutcomeUnknown _failure = new(new TimeoutException());
    Exception? _error;

    void Establish() => _fenced.Configure().Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>()).Returns(Task.FromException<Result<AppendedEvent, DuplicateEventSequenceNumber>>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(AppendProtected);

    [Fact] void should_preserve_the_unknown_outcome() => _error.ShouldEqual(_failure);
    [Fact] void should_not_retry_the_commit() => _fenced.Received(1).Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_not_fall_back_to_tokenless_storage() => TokenlessAppendCount.ShouldEqual(0);
    [Fact] void should_not_settle_by_tokenless_read_back() => _eventSequenceStorage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "ReadBackAppend").ShouldEqual(0);
}
