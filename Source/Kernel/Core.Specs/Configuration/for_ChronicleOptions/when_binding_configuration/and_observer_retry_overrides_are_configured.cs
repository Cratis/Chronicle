// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

[Collection("configuration bootstrap")]
public class and_observer_retry_overrides_are_configured : given.configured_retry_options
{
    void Establish() => Configure("""
        { "Cratis": { "Chronicle": {
          "Observers": { "BackoffDelay": 7, "ExponentialBackoffDelayFactor": 3, "MaximumBackoffDelay": 41 },
          "ObserverRetryOverrides": {
            "retry-binding#store#namespace#sequence": { "MaxRetryAttempts": 2, "BackoffDelay": 5, "ExponentialBackoffDelayFactor": 4, "MaximumBackoffDelay": 20 },
            "retry-partial#store#namespace#sequence": { "MaxRetryAttempts": 0 }
          }
        } } }
        """);

    async Task Because() => _result = await _provider.GetFor("retry-partial#store#namespace#sequence");

    [Fact] void should_bind_the_full_entry() => _monitor.CurrentValue.ObserverRetryOverrides[Key].MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_leave_omitted_fields_nullable() => _monitor.CurrentValue.ObserverRetryOverrides["retry-partial#store#namespace#sequence"].BackoffDelay.ShouldBeNull();
    [Fact] void should_resolve_partial_entries_using_bound_globals() => (_result.MaxRetryAttempts, _result.BackoffDelay, _result.ExponentialBackoffDelayFactor, _result.MaximumBackoffDelay).ShouldEqual((0, 7, 3f, 41));
    [Fact] async Task should_resolve_the_full_bound_entry()
    {
        var result = await _provider.GetFor(Key);
        (result.MaxRetryAttempts, result.BackoffDelay, result.ExponentialBackoffDelayFactor, result.MaximumBackoffDelay).ShouldEqual((2, 5, 4f, 20));
    }
}
