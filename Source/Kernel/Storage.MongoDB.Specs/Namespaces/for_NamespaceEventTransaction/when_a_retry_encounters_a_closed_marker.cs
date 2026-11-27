// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_a_retry_encounters_a_closed_marker : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _onFenceFailure = () => _matches = 0;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_recheck_admission_instead_of_refreshing_the_identity() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_not_keep_retrying_a_stale_identity() => _attempts.ShouldEqual(2);
    [Fact] void should_not_write_events() => _writeSessions.ShouldBeEmpty();
    [Fact] void should_abort_both_transactions() => _aborts.ShouldEqual(2);
}
