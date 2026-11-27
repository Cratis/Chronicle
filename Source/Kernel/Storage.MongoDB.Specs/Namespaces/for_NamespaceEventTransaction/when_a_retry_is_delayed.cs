// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_a_retry_is_delayed : given.a_namespace_transaction
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 2;
    }

    Task Because() => Execute();

    [Fact] void should_delay_only_the_retries() => _delays.ShouldEqual<TimeSpan>([TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(25)]);
    [Fact] void should_open_exactly_three_transactions() => _attempts.ShouldEqual(3);
    [Fact] void should_commit_only_the_winner() => _commits.ShouldEqual(1);
    [Fact] void should_abort_before_each_retry() => _steps.SequenceEqual(["fence", "abort", "fence", "abort", "fence", "write", "commit"]).ShouldBeTrue();
}
