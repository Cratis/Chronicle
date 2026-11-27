// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_the_attempt_budget_is_exhausted : given.a_retry_budget
{
    async Task Because()
    {
        using var lifetime = _retries.Bind(_context);
        await _retries.BeginAttempt(lifetime);
        await _retries.BeginAttempt(lifetime);
        await _retries.BeginAttempt(lifetime);
        _error = await Catch.Exception(() => _retries.BeginAttempt(lifetime));
    }

    [Fact] void should_have_no_fourth_attempt() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_report_exhaustion() => _retries.HasRemainingAttempts.ShouldBeFalse();
    [Fact] void should_delay_only_the_two_allowed_retries() => _delays.Count.ShouldEqual(2);
}
