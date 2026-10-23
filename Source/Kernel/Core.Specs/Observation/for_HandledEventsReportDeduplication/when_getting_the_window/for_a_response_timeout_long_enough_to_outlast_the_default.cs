// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_HandledEventsReportDeduplication.when_getting_the_window;

public class for_a_response_timeout_long_enough_to_outlast_the_default : Specification
{
    static readonly TimeSpan _responseTimeout = TimeSpan.FromMinutes(10);
    TimeSpan _window;

    void Because() => _window = HandledEventsReportDeduplication.WindowFor(_responseTimeout);

    [Fact] void should_outlast_every_attempt_of_a_report() =>
        (_window > (_responseTimeout + TransientTimeouts.DefaultRetryDelay) * TransientTimeouts.MaximumAttempts).ShouldBeTrue();
}
