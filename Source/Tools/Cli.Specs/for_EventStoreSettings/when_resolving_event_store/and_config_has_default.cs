// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_EventStoreSettings.when_resolving_event_store;

[Collection(CliSpecsCollection.Name)]
public class and_config_has_default : given.a_temp_config_directory
{
    const string ExpectedEventStore = "configured-store";

    EventStoreSettings _settings;
    string _result;

    void Establish()
    {
        var config = new CliConfiguration { DefaultEventStore = ExpectedEventStore };
        config.Save();
        _settings = new EventStoreSettings();
    }

    void Because() => _result = _settings.ResolveEventStore();

    [Fact] void should_return_the_config_value() => _result.ShouldEqual(ExpectedEventStore);
}
