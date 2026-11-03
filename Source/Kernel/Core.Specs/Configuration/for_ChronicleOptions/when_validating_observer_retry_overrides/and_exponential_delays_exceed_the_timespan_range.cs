// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

public class and_exponential_delays_exceed_the_timespan_range : given.configured_retry_options
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void should_accept_finite_factors_without_an_attempt_range_restriction(int limit) => Validate(new ObserverRetryOverrides
    {
        MaxRetryAttempts = limit,
        BackoffDelay = int.MaxValue,
        ExponentialBackoffDelayFactor = float.MaxValue,
        MaximumBackoffDelay = 10
    }).Length.ShouldEqual(0);
}
