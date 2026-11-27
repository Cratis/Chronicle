// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_commit_definitely_aborts_with_a_write_conflict : given.a_namespace_transaction
{
    void Establish()
    {
        _commitFailure = CommandFailure(112);
        _commitFailures = 1;
    }

    Task Because() => Execute();

    [Fact] void should_retry_from_the_fence_not_just_commit() => _steps.SequenceEqual(["fence", "write", "commit", "abort", "fence", "write", "commit"]).ShouldBeTrue();
    [Fact] void should_use_a_fresh_transaction() => _attempts.ShouldEqual(2);
    [Fact] void should_return_only_the_known_commit() => _result.ShouldEqual(42);
}
