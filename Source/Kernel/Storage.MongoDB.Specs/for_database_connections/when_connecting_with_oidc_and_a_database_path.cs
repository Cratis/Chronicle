// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections;

public class when_connecting_with_oidc_and_a_database_path : given.all_dependencies
{
    void Establish() => _server = "mongodb://localhost:27017/identity?authMechanism=MONGODB-OIDC";

    void Because() => Connect();

    [Fact] void should_keep_the_external_authentication_source() => _settings.All(settings => settings.Credential.Source == "$external").ShouldBeTrue();
}
