// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_adding_configuration;

[Collection("configuration bootstrap")]
public class and_environment_overrides_conflicting_host_values : given.configured_retry_options
{
    string _key;

    void Establish()
    {
        _key = $"retry-env-{Guid.NewGuid():N}#store#namespace#sequence";
        SetEnvironment($"Cratis__Chronicle__ObserverRetryOverrides__{_key}__MaxRetryAttempts", "3");
        Configure($$"""
            { "Cratis": { "Chronicle": {
              "Observers": { "BackoffDelay": 7, "MaximumBackoffDelay": 43 },
              "ObserverRetryOverrides": { "{{_key}}": { "MaxRetryAttempts": 9, "BackoffDelay": 5 } }
            } } }
            """);
    }

    async Task Because() => _result = await _provider.GetFor(_key);

    [Fact] void should_copy_the_environment_leaf_over_conflicting_json() => _result.MaxRetryAttempts.ShouldEqual(3);
    [Fact] void should_keep_json_only_entry_fields() => _result.BackoffDelay.ShouldEqual(5);
    [Fact] void should_inherit_json_only_globals() => _result.MaximumBackoffDelay.ShouldEqual(43);
    [Fact] void should_bind_the_environment_value() => _monitor.CurrentValue.ObserverRetryOverrides[_key].MaxRetryAttempts.ShouldEqual(3);
}
