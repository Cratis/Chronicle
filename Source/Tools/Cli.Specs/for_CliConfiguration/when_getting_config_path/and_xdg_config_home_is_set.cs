// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_CliConfiguration.when_getting_config_path;

[Collection(CliSpecsCollection.Name)]
public class and_xdg_config_home_is_set : given.a_temp_config_directory
{
    string _result;

    void Because() => _result = CliConfiguration.GetConfigPath();

    [Fact] void should_use_xdg_config_home() => _result.ShouldContain(_tempConfigHome);
    [Fact] void should_end_with_cratis_config_json() => _result.ShouldContain(Path.Combine("cratis", "config.json"));
}
