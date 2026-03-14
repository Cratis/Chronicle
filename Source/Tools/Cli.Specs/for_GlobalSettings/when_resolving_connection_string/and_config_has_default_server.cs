// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_config_has_default_server : given.a_temp_config_directory
{
    const string ExpectedServer = "chronicle://config-host:9999";

    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", null);
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { Server = ExpectedServer }
            }
        };
        config.Save();
        _settings = new GlobalSettings();
    }

    void Because() => _result = _settings.ResolveConnectionString();

    [Fact] void should_return_the_config_value() => _result.ShouldEqual(ExpectedServer);
}
