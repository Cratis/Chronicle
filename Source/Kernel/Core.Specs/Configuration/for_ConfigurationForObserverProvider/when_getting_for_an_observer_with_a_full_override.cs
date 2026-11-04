// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_getting_for_an_observer_with_a_full_override : given.a_provider_with_retry_overrides
{
    Observers _result;
    Observers _unrelated;
    Observers _bareId;
    Observers _differentCase;

    async Task Because()
    {
        _result = await _provider.GetFor(FullKey);
        _unrelated = await _provider.GetFor("other#my-store#Default#event-log");
        _bareId = await _provider.GetFor("full");
        _differentCase = await _provider.GetFor(FullKey.ToUpperInvariant());
    }

    [Fact] void should_apply_the_overridden_max_retry_attempts() => _result.MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_apply_the_overridden_backoff_delay() => _result.BackoffDelay.ShouldEqual(5);
    [Fact] void should_apply_the_overridden_backoff_factor() => _result.ExponentialBackoffDelayFactor.ShouldEqual(3f);
    [Fact] void should_apply_the_overridden_maximum_backoff_delay() => _result.MaximumBackoffDelay.ShouldEqual(120);
    [Fact] void should_use_the_global_settings_for_an_observer_without_an_override() => _unrelated.ShouldBeSame(_global);
    [Fact] void should_not_match_a_bare_observer_id() => _bareId.ShouldBeSame(_global);
    [Fact] void should_not_match_a_key_in_a_different_case() => _differentCase.ShouldBeSame(_global);
    [Fact] void should_not_change_the_global_max_retry_attempts() => _global.MaxRetryAttempts.ShouldEqual(23);
    [Fact] void should_not_change_the_global_backoff_delay() => _global.BackoffDelay.ShouldEqual(3);
    [Fact] void should_not_change_the_global_backoff_factor() => _global.ExponentialBackoffDelayFactor.ShouldEqual(1.5f);
    [Fact] void should_not_change_the_global_maximum_backoff_delay() => _global.MaximumBackoffDelay.ShouldEqual(900);
}
