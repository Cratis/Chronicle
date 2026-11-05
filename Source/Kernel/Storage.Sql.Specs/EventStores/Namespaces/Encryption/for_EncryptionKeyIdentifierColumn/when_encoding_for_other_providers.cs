// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierColumn;

public class when_encoding_for_other_providers : Specification
{
    static readonly string[] _identifiers = ["", "alice", "alice ", " ", "alice\u0001"];
    string[] _sqlite;
    string[] _postgresql;

    void Because()
    {
        using var sqlite = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite("DataSource=:memory:").Options);
        using var postgresql = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>().UseNpgsql("Host=localhost;Database=encoding").Options);
        _sqlite = [.. _identifiers.Select(_ => EncryptionKeyIdentifierColumn.Value(sqlite, new EncryptionKeyIdentifier(_)))];
        _postgresql = [.. _identifiers.Select(_ => EncryptionKeyIdentifierColumn.Value(postgresql, new EncryptionKeyIdentifier(_)))];
    }

    [Fact] void should_store_identifiers_unchanged_on_sqlite() => _sqlite.ShouldEqual(_identifiers);
    [Fact] void should_store_identifiers_unchanged_on_postgresql() => _postgresql.ShouldEqual(_identifiers);
}
