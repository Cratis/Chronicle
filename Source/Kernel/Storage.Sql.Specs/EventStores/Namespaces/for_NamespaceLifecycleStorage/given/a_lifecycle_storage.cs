// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceLifecycleStorage.given;

public class a_lifecycle_storage : a_sqlite_database
{
    protected readonly EventStoreNamespaceName _name = "Orders";
    protected readonly NamespaceIncarnationId _incarnation = NamespaceIncarnationId.New();
    protected readonly DateTimeOffset _now = new(2026, 5, 20, 12, 30, 0, TimeSpan.FromHours(2));
    protected INamespaceLifecycleStorage _storage;

    void Establish() => _storage = CreateStorage();

    protected INamespaceLifecycleStorage CreateStorage() => new NamespaceLifecycleStorage(() => _database.EventStore("lifecycle-store"));

    protected NamespaceLifecycle Open() => NamespaceLifecycle.Open(_name, _incarnation, _now);
}
