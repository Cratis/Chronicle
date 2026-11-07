// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierColumn;

public class when_encoding_for_sql_server : Specification
{
    static readonly string[] _identifiers = ["Alice", "alice", "alice ", "ALICE  ", "Ålesund", "ålesund", "ａｌｉｃｅ", "alice\u0001", "alice \u0001"];
    NamespaceDbContext _context;
    string[] _encoded;

    void Establish() => _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlServer("Server=localhost;Database=encoding;TrustServerCertificate=True").Options);

    void Because() => _encoded = [.. _identifiers.Select(_ => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier(_)))];

    [Fact] void should_leave_the_empty_identifier_unchanged() => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier(string.Empty)).ShouldEqual(string.Empty);
    [Fact] void should_leave_an_identifier_without_trailing_space_unchanged() => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier("alice")).ShouldEqual("alice");
    [Fact] void should_terminate_an_identifier_with_trailing_space() => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier("alice ")).ShouldEqual("alice \u0001");
    [Fact] void should_terminate_a_single_space() => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier(" ")).ShouldEqual(" \u0001");
    [Fact] void should_terminate_an_identifier_that_ends_with_the_terminator() => EncryptionKeyIdentifierColumn.Value(_context, new EncryptionKeyIdentifier("alice\u0001")).ShouldEqual("alice\u0001\u0001");
    [Fact] void should_give_every_identifier_its_own_value() => _encoded.Distinct().Count().ShouldEqual(_identifiers.Length);
    [Fact] void should_never_end_with_a_space() => _encoded.Any(_ => _.EndsWith(' ')).ShouldBeFalse();
}
