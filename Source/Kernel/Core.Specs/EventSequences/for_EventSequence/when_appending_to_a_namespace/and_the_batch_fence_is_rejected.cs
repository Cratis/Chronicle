// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_the_batch_fence_is_rejected : given.a_namespace_bound_sequence
{
    readonly NamespaceCommitRejected _failure = new();
    Exception? _error;

    void Establish() => _fenced.Configure().AppendMany(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(Task.FromException<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>>(_failure));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(AppendBatchProtected);

    [Fact] void should_preserve_the_fence_rejection() => _error.ShouldEqual(_failure);
    [Fact] void should_not_retry_a_rejected_batch() => _fenced.Received(1).AppendMany(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_not_fall_back_to_tokenless_storage() => TokenlessAppendCount.ShouldEqual(0);
    [Fact] void should_not_report_a_successful_write() => _written.ShouldBeEmpty();
}
