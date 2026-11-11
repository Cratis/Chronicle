// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_JsonColumnDocuments;

public class when_coercing_values_for_columns : Specification
{
    static readonly Guid _guid = Guid.Parse("7d0ddf11-0a3c-4ba2-a2ff-e8f2c0f4a5d1");

    object? _null;
    object? _guidResult;
    object? _timestamp;
    object? _date;
    object? _time;
    object? _decimalResult;
    object? _concept;
    object? _unconvertible;
    object? _nullableInt;

    void Because()
    {
        _null = JsonColumnDocuments.CoerceForColumn(Column(typeof(int)), null);
        _guidResult = JsonColumnDocuments.CoerceForColumn(Column(typeof(Guid)), _guid.ToString());
        _timestamp = JsonColumnDocuments.CoerceForColumn(Column(typeof(DateTimeOffset)), "2026-03-04T05:06:07.0000000+02:00");
        _date = JsonColumnDocuments.CoerceForColumn(Column(typeof(DateOnly)), "2026-03-04");
        _time = JsonColumnDocuments.CoerceForColumn(Column(typeof(TimeOnly)), "05:06:07");
        _decimalResult = JsonColumnDocuments.CoerceForColumn(Column(typeof(decimal)), 1.5d);
        _concept = JsonColumnDocuments.CoerceForColumn(Column(typeof(string)), new given.a_value("text"));
        _unconvertible = JsonColumnDocuments.CoerceForColumn(Column(typeof(int)), "not-a-number");
        _nullableInt = JsonColumnDocuments.CoerceForColumn(Column(typeof(int?)), 42L);
    }

    [Fact] void should_keep_null() => _null.ShouldBeNull();
    [Fact] void should_parse_a_guid() => _guidResult.ShouldEqual(_guid);
    [Fact] void should_parse_a_timestamp() => _timestamp.ShouldEqual(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(2)));
    [Fact] void should_parse_a_date() => _date.ShouldEqual(new DateOnly(2026, 3, 4));
    [Fact] void should_parse_a_time() => _time.ShouldEqual(new TimeOnly(5, 6, 7));
    [Fact] void should_convert_to_a_decimal() => _decimalResult.ShouldEqual(1.5m);
    [Fact] void should_unwrap_a_concept() => _concept.ShouldEqual("text");
    [Fact] void should_keep_a_value_that_cannot_be_converted() => _unconvertible.ShouldEqual("not-a-number");
    [Fact] void should_convert_to_the_underlying_type_of_a_nullable_column() => _nullableInt.ShouldEqual(42);

    static ProjectedColumn Column(Type type) => new("column", type, false, false, false, true);
}
