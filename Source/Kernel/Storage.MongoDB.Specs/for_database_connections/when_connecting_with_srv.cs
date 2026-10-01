// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections;

public class when_connecting_with_srv : given.all_dependencies
{
    void Establish()
    {
        _server = "mongodb+srv://user:password@cluster.example.test/identity?authSource=credentials";
        _directConnection = null;
    }

    void Because() => Connect();

    [Fact] void should_preserve_the_original_srv_settings() => _settings.TrueForAll(settings => settings.Equals(MongoClientSettings.FromUrl(new MongoUrl(_server)))).ShouldBeTrue();
    [Fact] void should_preserve_the_authentication_source() => _settings.TrueForAll(settings => settings.Credential.Source == "credentials").ShouldBeTrue();
}
