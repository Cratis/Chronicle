// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections;

public class when_connecting_with_multiple_hosts : given.all_dependencies
{
    void Establish()
    {
        _server = "mongodb://user:password@first.example.test:27017,second.example.test:27018/identity?authSource=credentials&replicaSet=chronicle&tls=true&retryWrites=true";
        _directConnection = null;
    }

    void Because() => Connect();

    [Fact] void should_preserve_the_original_connection_settings() => _settings.TrueForAll(settings => settings.Equals(MongoClientSettings.FromUrl(new MongoUrl(_server)))).ShouldBeTrue();
    [Fact] void should_preserve_both_hosts() => _settings.TrueForAll(settings => settings.Servers.SequenceEqual([new MongoServerAddress("first.example.test", 27017), new MongoServerAddress("second.example.test", 27018)])).ShouldBeTrue();
    [Fact] void should_preserve_tls() => _settings.TrueForAll(settings => settings.UseTls).ShouldBeTrue();
    [Fact] void should_not_force_a_direct_connection() => _settings.TrueForAll(settings => !settings.DirectConnection).ShouldBeTrue();
    [Fact] void should_preserve_the_authentication_source() => _settings.TrueForAll(settings => settings.Credential.Source == "credentials").ShouldBeTrue();
}
