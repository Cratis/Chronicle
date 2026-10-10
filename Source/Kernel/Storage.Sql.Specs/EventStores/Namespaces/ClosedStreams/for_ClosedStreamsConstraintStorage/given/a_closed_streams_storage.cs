// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.given;

public class a_closed_streams_storage : Namespaces.given.a_migrated_namespace_database
{
    protected ClosedStreamsConstraintStorage _storage;

    void Establish() => _storage = new(_eventStore, _namespace, EventSequenceId.Log, _database);
}
