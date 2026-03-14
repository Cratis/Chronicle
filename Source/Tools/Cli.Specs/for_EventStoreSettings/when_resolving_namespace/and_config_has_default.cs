// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_EventStoreSettings.when_resolving_namespace;

[Collection(CliSpecsCollection.Name)]
public class and_config_has_default : given.a_temp_config_directory
{
    const string ExpectedNamespace = "configured-namespace";

    EventStoreSettings _settings;
    string _result;

    void Establish()
    {
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { Namespace = ExpectedNamespace }
            }
        };
        config.Save();
        _settings = new EventStoreSettings();
    }

    void Because() => _result = _settings.ResolveNamespace();

    [Fact] void should_return_the_config_value() => _result.ShouldEqual(ExpectedNamespace);
}
