// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

public class and_delays_are_invalid : given.configured_retry_options
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void should_reject_nonpositive_base_delays(int delay) => Validate(new ObserverRetryOverrides { BackoffDelay = delay }).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{Key}'].BackoffDelay");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void should_reject_nonpositive_caps(int delay) => Validate(new ObserverRetryOverrides { MaximumBackoffDelay = delay }).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{Key}'].MaximumBackoffDelay");

    [Fact] void should_validate_inherited_delays() => Validate(new ObserverRetryOverrides(), new Observers { BackoffDelay = 0, MaximumBackoffDelay = 0 }).Length.ShouldEqual(2);
    [Fact] void should_allow_replacing_invalid_global_delays() => Validate(new ObserverRetryOverrides { BackoffDelay = 5, MaximumBackoffDelay = 3 }, new Observers { BackoffDelay = 0, MaximumBackoffDelay = 0 }).Length.ShouldEqual(0);
}
