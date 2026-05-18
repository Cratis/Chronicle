// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_direct_connection_settings;

public class when_creating_event_store_namespace_database_with_connection_string_direct_connection_true : given.mongodb_client_manager_context
{
    void Because() => _ = new EventStoreNamespaceDatabase(
        new EventStoreName("event-store"),
        new EventStoreNamespaceName("namespace"),
        _clientManager,
        _mongoDBOptions);

    [Fact] void should_keep_direct_connection_enabled() => _settings.DirectConnection.ShouldBeTrue();
}
