// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_flag_takes_precedence_over_env_and_config : given.a_temp_config_directory
{
    const string FlagServer = "chronicle://flag:1111";
    const string EnvServer = "chronicle://env:2222";
    const string ConfigServer = "chronicle://config:3333";

    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", EnvServer);
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { Server = ConfigServer }
            }
        };
        config.Save();
        _settings = new GlobalSettings { Server = FlagServer };
    }

    void Because() => _result = _settings.ResolveConnectionString();

    [Fact] void should_return_the_flag_value() => _result.ShouldEqual(FlagServer);

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", null);
        base.CleanUp();
    }
}
