// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_the_first_attempt_is_admitted : given.a_retry_budget
{
    async Task Because()
    {
        using var lifetime = _retries.Bind(_context);
        await _retries.BeginAttempt(lifetime);
    }

    [Fact] void should_not_delay_the_first_attempt() => _delays.ShouldBeEmpty();
    [Fact] void should_leave_retry_capacity() => _retries.HasRemainingAttempts.ShouldBeTrue();
}
