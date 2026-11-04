// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration.for_ChronicleOptions.given;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

/// <summary>
/// Observer retry overrides bind from configuration by full observer key, and an environment variable wins over the
/// in-memory value. Global observer values are not validated.
/// </summary>
public class and_observer_retry_overrides_are_configured : configured_observer_retry_overrides
{
    const string BoundariesKey = "boundaries#my-store#Default#event-log";
    static readonly string _key = $"{Guid.NewGuid():N}-reactor#my-store#Default#event-log";

    ChronicleOptions _resolved;
    Exception _error;

    void Establish()
    {
        var maxRetryAttemptsName = $"Cratis__Chronicle__ObserverRetryOverrides__{_key}__MaxRetryAttempts";
        var maximumBackoffDelayName = $"Cratis__Chronicle__ObserverRetryOverrides__{_key}__MaximumBackoffDelay";
        var previousMaxRetryAttempts = Environment.GetEnvironmentVariable(maxRetryAttemptsName);
        var previousMaximumBackoffDelay = Environment.GetEnvironmentVariable(maximumBackoffDelayName);
        Environment.SetEnvironmentVariable(maxRetryAttemptsName, "7");
        Environment.SetEnvironmentVariable(maximumBackoffDelayName, "120");

        try
        {
            Configure(new Dictionary<string, string?>
            {
                [$"Cratis:Chronicle:ObserverRetryOverrides:{_key}:MaxRetryAttempts"] = "3",
                [$"Cratis:Chronicle:ObserverRetryOverrides:{_key}:ExponentialBackoffDelayFactor"] = "1.5",
                [$"Cratis:Chronicle:ObserverRetryOverrides:{BoundariesKey}:MaxRetryAttempts"] = "0",
                [$"Cratis:Chronicle:ObserverRetryOverrides:{BoundariesKey}:ExponentialBackoffDelayFactor"] = "1",
                ["Cratis:Chronicle:Observers:MaximumBackoffDelay"] = "0",
                ["Cratis:Chronicle:Observers:ExponentialBackoffDelayFactor"] = "0"
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable(maxRetryAttemptsName, previousMaxRetryAttempts);
            Environment.SetEnvironmentVariable(maximumBackoffDelayName, previousMaximumBackoffDelay);
        }
    }

    void Because()
    {
        _resolved = _options;
        _error = Catch.Exception(_startupValidator.Validate);
    }

    [Fact] void should_let_the_environment_variable_win_for_max_retry_attempts() => _resolved.ObserverRetryOverrides[_key].MaxRetryAttempts.ShouldEqual(7);
    [Fact] void should_bind_a_value_that_is_only_in_memory() => _resolved.ObserverRetryOverrides[_key].ExponentialBackoffDelayFactor.ShouldEqual(1.5f);
    [Fact] void should_bind_a_value_that_is_only_in_the_environment() => _resolved.ObserverRetryOverrides[_key].MaximumBackoffDelay.ShouldEqual(120);
    [Fact] void should_leave_an_unset_value_null() => _resolved.ObserverRetryOverrides[_key].BackoffDelay.ShouldBeNull();
    [Fact] void should_bind_unlimited_retry_attempts() => _resolved.ObserverRetryOverrides[BoundariesKey].MaxRetryAttempts.ShouldEqual(0);
    [Fact] void should_bind_the_lowest_backoff_factor() => _resolved.ObserverRetryOverrides[BoundariesKey].ExponentialBackoffDelayFactor.ShouldEqual(1f);
    [Fact] void should_pass_startup_validation() => _error.ShouldBeNull();
    [Fact] void should_match_keys_case_sensitively() => _resolved.ObserverRetryOverrides.ContainsKey(_key.ToUpperInvariant()).ShouldBeFalse();
}
