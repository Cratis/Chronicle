// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamColumn;

public class when_encoding_for_sql_server : Specification
{
    static readonly string[] _values = ["", "Order-1", "order-1", "Order-1 ", "ORDER-1  ", "Ørder-1", "ＯＲＤＥＲ-1", " ", "Order-1\u007F", "Order-1 \u007F", "Order-1\u007F "];
    NamespaceDbContext _context;
    string[] _encoded;
    string[] _decoded;

    void Establish() => _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlServer("Server=localhost;Database=encoding;TrustServerCertificate=True").Options);

    void Because()
    {
        _encoded = [.. _values.Select(_ => ClosedStreamColumn.Encode(_context, _))];
        _decoded = [.. _encoded.Select(_ => ClosedStreamColumn.Decode(_context, _))];
    }

    [Fact] void should_leave_the_empty_value_unchanged() => ClosedStreamColumn.Encode(_context, string.Empty).ShouldEqual(string.Empty);
    [Fact] void should_leave_a_value_without_trailing_space_unchanged() => ClosedStreamColumn.Encode(_context, "Order-1").ShouldEqual("Order-1");
    [Fact] void should_terminate_a_value_with_trailing_space() => ClosedStreamColumn.Encode(_context, "Order-1 ").ShouldEqual("Order-1 \u007F");
    [Fact] void should_terminate_a_single_space() => ClosedStreamColumn.Encode(_context, " ").ShouldEqual(" \u007F");
    [Fact] void should_terminate_a_value_that_ends_with_the_terminator() => ClosedStreamColumn.Encode(_context, "Order-1\u007F").ShouldEqual("Order-1\u007F\u007F");
    [Fact] void should_give_every_value_its_own_stored_form() => _encoded.Distinct().Count().ShouldEqual(_values.Length);
    [Fact] void should_never_store_a_trailing_space() => _encoded.Any(_ => _.EndsWith(' ')).ShouldBeFalse();
    [Fact] void should_decode_every_value_back_to_its_spelling() => _decoded.ShouldEqual(_values);
}
