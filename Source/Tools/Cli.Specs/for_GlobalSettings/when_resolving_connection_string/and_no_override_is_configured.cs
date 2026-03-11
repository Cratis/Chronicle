// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_no_override_is_configured : given.a_temp_config_directory
{
    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_CONNECTION_STRING", null);
        _settings = new GlobalSettings();
    }

    void Because() => _result = _settings.ResolveConnectionString();

    [Fact] void should_return_the_development_default() => _result.ShouldContain(ChronicleConnectionString.DevelopmentClient);
    [Fact] void should_target_localhost() => _result.ShouldContain("localhost:35000");
}
