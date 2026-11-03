// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_adding_configuration;

[Collection("configuration bootstrap")]
public class and_environment_changes_after_bootstrap : given.configured_retry_options
{
    string _key;
    string _variable;
    Observers _before;

    async Task Establish()
    {
        _key = $"retry-snapshot-{Guid.NewGuid():N}#store#namespace#sequence";
        _variable = $"Cratis__Chronicle__ObserverRetryOverrides__{_key}__MaxRetryAttempts";
        SetEnvironment(_variable, "2");
        Configure();
        _before = await _provider.GetFor(_key);
    }

    async Task Because()
    {
        SetEnvironment(_variable, "9");
        _configuration.Reload();
        _result = await _provider.GetFor(_key);
    }

    [Fact] void should_initially_bind_the_bootstrap_environment() => _before.MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_not_reread_the_environment_on_host_reload() => _result.MaxRetryAttempts.ShouldEqual(2);
    [Fact] void should_keep_the_copied_host_leaf() => _configuration[$"Cratis:Chronicle:ObserverRetryOverrides:{_key}:MaxRetryAttempts"].ShouldEqual("2");
    [Fact] void should_have_changed_only_the_process_environment() => Environment.GetEnvironmentVariable(_variable).ShouldEqual("9");
}
