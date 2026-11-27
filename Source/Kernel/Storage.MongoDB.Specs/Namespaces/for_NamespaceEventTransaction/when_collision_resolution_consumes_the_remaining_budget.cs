// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_collision_resolution_consumes_the_remaining_budget : given.a_namespace_transaction
{
    async Task Because()
    {
        _writeFailure = CommandFailure(11000);
        await ExecuteCatching();
        _writeFailure = null!;
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 4;
        await ExecuteCatching();
    }

    [Fact] void should_not_reset_the_three_attempt_budget() => _attempts.ShouldEqual(3);
    [Fact] void should_preserve_the_final_definite_conflict() => _failure.ShouldBeSame(_fenceFailure);
    [Fact] void should_delay_only_two_recovery_attempts() => _delays.Count.ShouldEqual(2);
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
}
