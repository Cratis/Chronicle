// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_deciding_whether_to_retry_a_step_that_timed_out : given.a_step_that_ended_with_a_timeout
{
    bool _firstAttempt;
    bool _lastAttemptAllowed;
    bool _afterTheLastAttempt;

    void Because()
    {
        _firstAttempt = TransientTimeouts.ShouldRetry(_timedOut, 1, _options);
        _lastAttemptAllowed = TransientTimeouts.ShouldRetry(_timedOut, TransientTimeouts.MaximumAttempts - 1, _options);
        _afterTheLastAttempt = TransientTimeouts.ShouldRetry(_timedOut, TransientTimeouts.MaximumAttempts, _options);
    }

    [Fact] void should_retry_the_first_attempt() => _firstAttempt.ShouldBeTrue();
    [Fact] void should_retry_until_the_last_attempt() => _lastAttemptAllowed.ShouldBeTrue();
    [Fact] void should_not_retry_once_attempts_are_used_up() => _afterTheLastAttempt.ShouldBeFalse();
}
