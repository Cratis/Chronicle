// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections;

public class when_connecting_without_a_database_path : given.all_dependencies
{
    void Establish() => _server = "mongodb://user:password@localhost:27017";

    void Because() => Connect();

    [Fact] void should_keep_admin_as_the_authentication_source() => _settings.All(settings => settings.Credential.Source == "admin").ShouldBeTrue();
    [Fact] void should_preserve_direct_connection() => _settings.All(settings => settings.DirectConnection == true).ShouldBeTrue();
    [Fact] void should_select_the_prefixed_event_store() => _client.Received().GetDatabase("run_Ada+es", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_select_the_prefixed_namespace() => _client.Received().GetDatabase("run_Ada+es+tenant", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_select_the_prefixed_read_models() => _client.Received().GetDatabase("run_Ada+tenant", Arg.Any<MongoDatabaseSettings>());
}
