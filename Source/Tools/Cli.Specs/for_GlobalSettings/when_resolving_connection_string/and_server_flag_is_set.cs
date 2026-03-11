// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_server_flag_is_set : given.a_temp_config_directory
{
    const string ExpectedServer = "chronicle://flag-host:1234";

    GlobalSettings _settings;
    string _result;

    void Establish() => _settings = new GlobalSettings { Server = ExpectedServer };

    void Because() => _result = _settings.ResolveConnectionString();

    [Fact] void should_return_the_flag_value() => _result.ShouldEqual(ExpectedServer);
}
