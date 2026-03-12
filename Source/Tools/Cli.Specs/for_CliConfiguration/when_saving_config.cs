// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_saving_config : given.a_temp_config_directory
{
    const string ExpectedServer = "chronicle://saved-host:8080";

    CliConfiguration _loaded;

    void Establish()
    {
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { Server = ExpectedServer }
            }
        };
        config.Save();
    }

    void Because() => _loaded = CliConfiguration.Load();

    [Fact] void should_persist_the_config_file() => File.Exists(CliConfiguration.GetConfigPath()).ShouldBeTrue();
    [Fact] void should_roundtrip_the_server() => _loaded.GetCurrentContext().Server.ShouldEqual(ExpectedServer);
}
