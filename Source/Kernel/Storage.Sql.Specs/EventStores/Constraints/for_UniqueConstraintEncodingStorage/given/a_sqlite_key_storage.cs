// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.given;

public class a_sqlite_key_storage : a_sqlite_database
{
    protected IUniqueConstraintEncodingStorage _storage;

    void Establish() => _storage = CreateStorage();

    protected IUniqueConstraintEncodingStorage CreateStorage() => new EventStores.Constraints.UniqueConstraintEncodingStorage(() => _database.EventStore("key-store"));
}
