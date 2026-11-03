// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

public class and_factors_are_invalid : given.configured_retry_options
{
    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void should_reject_invalid_factors(float factor) => Validate(new ObserverRetryOverrides { ExponentialBackoffDelayFactor = factor }).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{Key}'].ExponentialBackoffDelayFactor");

    [Fact] void should_validate_an_inherited_factor() => Validate(new ObserverRetryOverrides(), new Observers { ExponentialBackoffDelayFactor = 0 }).Length.ShouldEqual(1);
    [Fact] void should_allow_replacing_an_invalid_global_factor() => Validate(new ObserverRetryOverrides { ExponentialBackoffDelayFactor = 1 }, new Observers { ExponentialBackoffDelayFactor = 0 }).Length.ShouldEqual(0);
}
