// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_an_override_is_partial : given.a_provider
{
    void Establish() => Configure(new Dictionary<string, ObserverRetryOverrides> { [ObserverKey] = new() { MaxRetryAttempts = 0 } });

    async Task Because() => _result = await _provider.GetFor(ObserverKey);

    [Fact] void should_accept_an_unlimited_budget() => _result.MaxRetryAttempts.ShouldEqual(0);
    [Fact] void should_inherit_current_global_delays() => (_result.BackoffDelay, _result.ExponentialBackoffDelayFactor, _result.MaximumBackoffDelay).ShouldEqual((4, 3f, 90));
    [Fact] void should_preserve_all_unrelated_properties() => AssertUnrelatedPropertiesArePreserved();
}
