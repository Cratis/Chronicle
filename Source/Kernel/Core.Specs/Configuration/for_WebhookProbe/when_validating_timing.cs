// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_WebhookProbe;

public class when_validating_timing : Specification
{
    [Fact] void should_accept_defaults() => new WebhookProbe().Validate();
    [Fact] void should_reject_zero_initial_delay() => Catch.Exception(() => new WebhookProbe { InitialDelaySeconds = 0 }.Validate()).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_a_maximum_below_the_initial_delay() => Catch.Exception(() => new WebhookProbe { MaximumDelaySeconds = 30 }.Validate()).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_non_finite_backoff() => Catch.Exception(() => new WebhookProbe { BackoffFactor = double.NaN }.Validate()).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_unbounded_jitter() => Catch.Exception(() => new WebhookProbe { JitterRatio = 1 }.Validate()).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_non_positive_timeout() => Catch.Exception(() => new WebhookProbe { TimeoutSeconds = 0 }.Validate()).ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
