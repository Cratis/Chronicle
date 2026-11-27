// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_commit_has_an_unknown_result : given.a_namespace_transaction
{
    void Establish()
    {
        _commitFailure = CommandFailure(112, "UnknownTransactionCommitResult");
        _commitFailures = 1;
    }

    Task Because() => ExecuteCatching();

    [Fact] void should_report_unknown_even_when_the_code_is_write_conflict() => _failure.ShouldBeOfExactType<NamespaceCommitOutcomeUnknown>();
    [Fact] void should_preserve_the_commit_failure() => _failure.InnerException.ShouldEqual(_commitFailure);
    [Fact] void should_not_retry_the_transaction() => _attempts.ShouldEqual(1);
    [Fact] void should_not_retry_commit() => _commits.ShouldEqual(1);
    [Fact] void should_not_claim_to_abort() => _aborts.ShouldEqual(0);
}
