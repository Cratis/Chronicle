// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.given;

internal sealed class name_catalog : IReadModelTableCatalog
{
    internal readonly List<string> Names = [];
    internal StringComparer Comparer = StringComparer.Ordinal;
    public DatabaseType Provider { get; set; } = DatabaseType.PostgreSql;

    public Task<string?> Find(string name) => Task.FromResult(Names.SingleOrDefault(stored => Same(stored, name)));
    public Task<bool> Equal(string left, string right) => Task.FromResult(Same(left, right));

    bool Same(string left, string right) => Provider == DatabaseType.Sqlite ? ReadModelTableCatalog.SqliteIdentifiersEqual(left, right) : Comparer.Equals(left, right);
}
