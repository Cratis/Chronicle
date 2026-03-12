// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_EventStoreSettings.when_resolving_event_store;

[Collection(CliSpecsCollection.Name)]
public class and_no_override_is_configured : given.a_temp_config_directory
{
    EventStoreSettings _settings;
    string _result;

    void Establish() => _settings = new EventStoreSettings();

    void Because() => _result = _settings.ResolveEventStore();

    [Fact] void should_return_default() => _result.ShouldEqual(CliDefaults.DefaultEventStoreName);
}
