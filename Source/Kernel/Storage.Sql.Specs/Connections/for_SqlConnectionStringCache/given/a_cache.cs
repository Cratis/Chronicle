// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache.given;

public class a_cache : Specification
{
    protected const string Configured = "Host=pg.example;Database=chronicle;Username=app;Password=initial";
    private protected SqlConnectionStringCache _cache;
    private protected DatabaseIdentity _identity;
    private protected SqlConnectionStringInput _input;
    protected PostgreSqlPooling _pooling;

    void Establish()
    {
        _cache = new();
        _identity = new(DatabaseKind.Namespace, "store", "tenant");
        _pooling = new();
        _input = SqlConnectionStringInput.From(Configured, "tenant", _pooling);
    }

    private protected string Normalize(SqlConnectionStringInput input) => _cache.Get(_identity, ConnectionPurpose.Runtime, input);
}
