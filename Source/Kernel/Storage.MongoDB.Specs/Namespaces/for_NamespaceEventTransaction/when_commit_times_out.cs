// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_commit_times_out : given.a_namespace_transaction
{
    void Establish()
    {
        _commitFailure = new TimeoutException();
        _commitFailures = 1;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_not_infer_that_no_events_committed() => _failure.ShouldBeOfExactType<NamespaceCommitOutcomeUnknown>();
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
    [Fact] void should_not_abort_after_an_unknown_commit() => _aborts.ShouldEqual(0);
}
