// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_CliConfiguration.when_getting_config_path;

[Collection(CliSpecsCollection.Name)]
public sealed class and_xdg_config_home_is_not_set : Specification, IDisposable
{
    string _previousValue;
    string _result;

    void Establish()
    {
        _previousValue = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
    }

    void Because() => _result = CliConfiguration.GetConfigPath();

    [Fact] void should_not_use_xdg_config_folder() => _result.ShouldNotContain(Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar);
    [Fact] void should_end_with_dot_cratis_config_json() => _result.ShouldContain(Path.Combine(".cratis", "config.json"));

    /// <inheritdoc/>
    void IDisposable.Dispose() => Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _previousValue);
}
