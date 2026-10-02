// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreDatabase.when_connecting;

public class with_an_explicit_authentication_source : given.all_dependencies
{
    void Establish() => _options.Value.Server = "mongodb://user:password@localhost:27017/identity?authSource=credentials";

    void Because() => _ = new EventStoreDatabase("Ada", _clientManager, _options, _storageOptions);

    [Fact] void should_authenticate_against_the_explicit_database() => _settings.Credential.Source.ShouldEqual("credentials");
    [Fact] void should_select_the_prefixed_event_store_database() => _client.Received().GetDatabase("run_Ada+es", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_preserve_direct_connection() => _settings.DirectConnection.ShouldEqual(true);
}
