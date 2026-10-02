// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceDatabase.when_connecting;

public class with_an_implicit_authentication_source : given.all_dependencies
{
    void Establish() => _options.Value.Server = "mongodb://user:password@localhost:27017/identity";

    void Because() => _ = new EventStoreNamespaceDatabase("Ada", "Contoso", _clientManager, _options, _storageOptions);

    [Fact] void should_authenticate_against_the_original_database() => _settings.Credential.Source.ShouldEqual("identity");
    [Fact] void should_select_the_prefixed_namespace_database() => _client.Received().GetDatabase("run_Ada+es+Contoso", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_preserve_direct_connection() => _settings.DirectConnection.ShouldEqual(true);
}
