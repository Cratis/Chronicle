// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_loading_and_no_config_exists : given.a_temp_config_directory
{
    CliConfiguration _result;

    void Because() => _result = CliConfiguration.Load();

    [Fact] void should_return_a_configuration() => _result.ShouldNotBeNull();
    [Fact] void should_have_null_server() => _result.GetCurrentContext().Server.ShouldBeNull();
    [Fact] void should_have_null_event_store() => _result.GetCurrentContext().EventStore.ShouldBeNull();
    [Fact] void should_have_null_namespace() => _result.GetCurrentContext().Namespace.ShouldBeNull();
}
