// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.given;

public class a_postgresql_key_storage(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    protected IUniqueConstraintEncodingStorage _storage;

    void Establish() => _storage = CreateStorage();

    protected IUniqueConstraintEncodingStorage CreateStorage() => new EventStores.Constraints.UniqueConstraintEncodingStorage(() => Database.EventStore(Store));
}
