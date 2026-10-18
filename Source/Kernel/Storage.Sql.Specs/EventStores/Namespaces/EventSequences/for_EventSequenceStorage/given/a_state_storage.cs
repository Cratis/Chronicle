// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class a_state_storage : Namespaces.given.a_migrated_namespace_database
{
    protected EventSequenceStorage _storage;

    void Establish() => _storage = new(
        _eventStore,
        _namespace,
        EventSequenceId.Log,
        _database,
        Substitute.For<IIdentityStorage>(),
        Substitute.For<ILogger<EventSequenceStorage>>(),
        Substitute.For<Chronicle.Storage.EventTypes.IEventTypesStorage>(),
        new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
}
