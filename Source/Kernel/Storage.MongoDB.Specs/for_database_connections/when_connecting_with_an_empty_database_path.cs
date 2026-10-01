// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections;

public class when_connecting_with_an_empty_database_path : given.all_dependencies
{
    void Establish() => _server = "mongodb://user:password@localhost:27017/";

    void Because() => Connect();

    [Fact] void should_keep_admin_as_the_authentication_source() => _settings.All(settings => settings.Credential.Source == "admin").ShouldBeTrue();
}
