// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_getting_for_an_observer_with_a_partial_override : given.a_provider_with_retry_overrides
{
    static readonly string[] _retryProperties =
    [
        nameof(Observers.MaxRetryAttempts),
        nameof(Observers.BackoffDelay),
        nameof(Observers.ExponentialBackoffDelayFactor),
        nameof(Observers.MaximumBackoffDelay)
    ];

    Observers _result;

    async Task Because() => _result = await _provider.GetFor(PartialKey);

    [Fact] void should_apply_the_overridden_max_retry_attempts() => _result.MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_inherit_the_global_backoff_delay() => _result.BackoffDelay.ShouldEqual(_global.BackoffDelay);
    [Fact] void should_inherit_the_global_backoff_factor() => _result.ExponentialBackoffDelayFactor.ShouldEqual(_global.ExponentialBackoffDelayFactor);
    [Fact] void should_inherit_the_global_maximum_backoff_delay() => _result.MaximumBackoffDelay.ShouldEqual(_global.MaximumBackoffDelay);
    [Fact]
    void should_copy_every_other_setting_from_the_global_settings() => typeof(Observers).GetProperties()
        .Where(_ => !_retryProperties.Contains(_.Name))
        .All(_ => Equals(_.GetValue(_result), _.GetValue(_global)))
        .ShouldBeTrue();

    [Fact]
    void should_have_a_global_fixture_that_sets_every_setting_to_a_non_default_value() => typeof(Observers).GetProperties()
        .All(_ => !Equals(_.GetValue(_global), _.GetValue(new Observers())))
        .ShouldBeTrue();
}
