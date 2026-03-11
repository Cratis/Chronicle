// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_loading_existing_config : given.a_temp_config_directory
{
    const string ExpectedServer = "chronicle://myhost:9000";
    const string ExpectedEventStore = "production";
    const string ExpectedNamespace = "my-namespace";

    CliConfiguration _result;

    void Establish()
    {
        var config = new CliConfiguration
        {
            DefaultServer = ExpectedServer,
            DefaultEventStore = ExpectedEventStore,
            DefaultNamespace = ExpectedNamespace
        };
        config.Save();
    }

    void Because() => _result = CliConfiguration.Load();

    [Fact] void should_have_correct_default_server() => _result.DefaultServer.ShouldEqual(ExpectedServer);
    [Fact] void should_have_correct_default_event_store() => _result.DefaultEventStore.ShouldEqual(ExpectedEventStore);
    [Fact] void should_have_correct_default_namespace() => _result.DefaultNamespace.ShouldEqual(ExpectedNamespace);
}
