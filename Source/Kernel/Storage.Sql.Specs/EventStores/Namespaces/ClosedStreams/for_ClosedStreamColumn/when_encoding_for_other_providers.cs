// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamColumn;

public class when_encoding_for_other_providers : Specification
{
    static readonly string[] _values = ["", "Order-1", "Order-1 ", " ", "Order-1\u0001"];
    string[] _sqlite;
    string[] _postgresql;
    string[] _decoded;

    void Because()
    {
        using var sqlite = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite("DataSource=:memory:").Options);
        using var postgresql = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>().UseNpgsql("Host=localhost;Database=encoding").Options);
        _sqlite = [.. _values.Select(_ => ClosedStreamColumn.Encode(sqlite, _))];
        _postgresql = [.. _values.Select(_ => ClosedStreamColumn.Encode(postgresql, _))];
        _decoded = [.. _values.Select(_ => ClosedStreamColumn.Decode(sqlite, _))];
    }

    [Fact] void should_store_values_unchanged_on_sqlite() => _sqlite.ShouldEqual(_values);
    [Fact] void should_store_values_unchanged_on_postgresql() => _postgresql.ShouldEqual(_values);
    [Fact] void should_read_values_unchanged() => _decoded.ShouldEqual(_values);
}
