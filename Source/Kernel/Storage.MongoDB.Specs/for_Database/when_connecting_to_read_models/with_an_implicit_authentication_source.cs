// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_Database.when_connecting_to_read_models;

public class with_an_implicit_authentication_source : given.a_database
{
    void Establish() => _options.Server = "mongodb://user:password@localhost:27017/identity";

    void Because() => _database.GetReadModelDatabase("Ada", "Contoso");

    [Fact] void should_authenticate_against_the_original_database() => _settings.Credential.Source.ShouldEqual("identity");
    [Fact] void should_select_the_prefixed_read_model_database() => _client.Received().GetDatabase("run_Ada+Contoso", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_preserve_direct_connection() => _settings.DirectConnection.ShouldEqual(true);
}
