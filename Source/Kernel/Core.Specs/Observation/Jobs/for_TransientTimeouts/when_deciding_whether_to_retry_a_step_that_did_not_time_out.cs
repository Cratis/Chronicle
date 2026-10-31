// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_deciding_whether_to_retry_a_step_that_did_not_time_out : given.a_step_that_ended_with_a_timeout
{
    bool _retriesOtherFailure;
    bool _retriesSuccess;

    void Because()
    {
        _retriesOtherFailure = TransientTimeouts.ShouldRetry(_otherFailure, 1, _options);
        _retriesSuccess = TransientTimeouts.ShouldRetry(_success, 1, _options);
    }

    [Fact] void should_not_retry_a_failure_that_is_not_a_timeout() => _retriesOtherFailure.ShouldBeFalse();
    [Fact] void should_not_retry_a_success() => _retriesSuccess.ShouldBeFalse();
}
