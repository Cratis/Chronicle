// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_environment_variable_is_set : given.a_temp_config_directory
{
    const string ExpectedServer = "chronicle://env-host:5678";

    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", ExpectedServer);
        _settings = new GlobalSettings();
    }

    void Because() => _result = _settings.ResolveConnectionString();

    [Fact] void should_return_the_environment_variable_value() => _result.ShouldEqual(ExpectedServer);

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", null);
        base.CleanUp();
    }
}
