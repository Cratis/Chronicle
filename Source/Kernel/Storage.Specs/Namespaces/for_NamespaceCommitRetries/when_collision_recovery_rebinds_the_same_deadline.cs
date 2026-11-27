// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_collision_recovery_rebinds_the_same_deadline : given.a_retry_budget
{
    void Establish() => _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };

    async Task Because()
    {
        using (var initial = _retries.Bind(_context))
        {
            await _retries.BeginAttempt(initial);
            _clock.Advance(TimeSpan.FromMilliseconds(500));
        }
        using var collision = _retries.Bind(_context);
        _clock.Advance(TimeSpan.FromMilliseconds(500));
        _error = await Catch.Exception(() => _retries.BeginAttempt(collision));
    }

    [Fact] void should_not_renew_the_deadline() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_start_backoff_after_the_deadline() => _delays.ShouldBeEmpty();
}
