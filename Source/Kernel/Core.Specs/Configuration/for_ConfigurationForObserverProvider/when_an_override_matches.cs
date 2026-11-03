// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ObserverIdentity = Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_an_override_matches : given.a_provider
{
    void Establish() => Configure(new Dictionary<string, ObserverRetryOverrides>
    {
        [ObserverKey] = new() { MaxRetryAttempts = 2, BackoffDelay = 5, ExponentialBackoffDelayFactor = 4, MaximumBackoffDelay = 20 }
    });

    async Task Because() => _result = await _provider.GetFor(new ObserverIdentity("observer", "store", "namespace", "sequence").ToString());

    [Fact] void should_use_the_canonical_full_identity() => new ObserverIdentity("observer", "store", "namespace", "sequence").ToString().ShouldEqual("observer#store#namespace#sequence");
    [Fact] void should_override_the_attempt_limit() => _result.MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_override_the_base_delay() => _result.BackoffDelay.ShouldEqual(5);
    [Fact] void should_override_the_factor() => _result.ExponentialBackoffDelayFactor.ShouldEqual(4f);
    [Fact] void should_override_the_cap() => _result.MaximumBackoffDelay.ShouldEqual(20);
    [Fact] void should_preserve_all_unrelated_properties() => AssertUnrelatedPropertiesArePreserved();
    [Fact] void should_return_a_new_projection() => ReferenceEquals(_result, _defaults).ShouldBeFalse();
    [Fact] void should_not_mutate_globals() => (_defaults.MaxRetryAttempts, _defaults.BackoffDelay, _defaults.ExponentialBackoffDelayFactor, _defaults.MaximumBackoffDelay).ShouldEqual((8, 4, 3f, 90));
}
