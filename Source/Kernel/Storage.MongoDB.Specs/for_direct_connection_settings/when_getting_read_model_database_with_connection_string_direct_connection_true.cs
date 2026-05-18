// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_direct_connection_settings;

public class when_getting_read_model_database_with_connection_string_direct_connection_true : given.mongodb_client_manager_context
{
    Database _database = default!;

    void Establish() => _database = new Database(_clientManager, _mongoDBOptions, _customSerializers);

    void Because() => _ = _database.GetReadModelDatabase(new EventStoreName("event-store"), EventStoreNamespaceName.Default);

    [Fact] void should_keep_direct_connection_enabled() => _settings.DirectConnection.ShouldBeTrue();
}
