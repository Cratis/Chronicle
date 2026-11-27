// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_a_write_conflict_is_retried : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 2;
    }

    Task Because() => Execute();

    [Fact] void should_allow_three_total_attempts() => _attempts.ShouldEqual(3);
    [Fact] void should_use_a_new_session_on_each_attempt() => _sessions.Distinct().Count().ShouldEqual(3);
    [Fact] void should_fence_every_attempt() => _steps.SequenceEqual(["fence", "abort", "fence", "abort", "fence", "write", "commit"]).ShouldBeTrue();
    [Fact] void should_only_commit_the_successful_attempt() => _commits.ShouldEqual(1);
    [Fact] void should_return_the_committed_result() => _result.ShouldEqual(42);
}
