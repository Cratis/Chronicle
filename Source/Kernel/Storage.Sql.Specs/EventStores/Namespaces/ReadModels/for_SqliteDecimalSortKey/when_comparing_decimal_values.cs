// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Data.Sqlite;

#pragma warning disable CA2100 // Identifiers and computed SQL are internal constants, values are parameters.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_SqliteDecimalSortKey;

public class when_comparing_decimal_values : Specification
{
    SqliteConnection _connection;
    decimal[] _values;
    decimal[] _ordered;
    string?[] _sqlKeys;
    string[] _expectedKeys;
    string[] _zeroKeys;
    long _invalidKeys;

    void Establish()
    {
        var random = new Random(5200);
        _values = new[]
        {
            decimal.MinValue, decimal.MaxValue, -10m, -2m, -1m, -0.0000000000000000000000000001m,
            0m, 0.0000000000000000000000000001m, 1m, 1.0m, 1.00m, 2m, 9m, 10m,
            193.58m, 193.580m, 1234567890.123456789012345678m,
            0.1234567890123456789012345678m, 0.1234567890123456789012345679m
        }.Concat(Enumerable.Range(0, 100).Select(_ => new decimal(random.Next(), random.Next(), random.Next(), random.Next(2) == 1, (byte)random.Next(29)))).ToArray();
        _expectedKeys = _values.Select(SqliteDecimalSortKey.For).ToArray();
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    void Because()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"CREATE TABLE decimals(amount TEXT, ordering TEXT AS ({SqliteDecimalSortKey.SqlExpressionFor("\"amount\"")}) VIRTUAL)";
        command.ExecuteNonQuery();
        foreach (var value in _values.Select(value => value.ToString(CultureInfo.InvariantCulture)).Concat(["-0", "-0.000", "0.000"]))
        {
            command.CommandText = "INSERT INTO decimals(amount) VALUES ($value)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$value", value);
            command.ExecuteNonQuery();
        }
        command.CommandText = "SELECT ordering FROM decimals ORDER BY rowid";
        using (var reader = command.ExecuteReader())
        {
            var keys = new List<string?>();
            while (reader.Read()) keys.Add(reader.GetString(0));
            _sqlKeys = keys.Take(_values.Length).ToArray();
            _zeroKeys = keys.Skip(_values.Length).Select(key => key!).ToArray();
        }
        command.CommandText = "SELECT amount FROM decimals ORDER BY ordering COLLATE BINARY";
        using (var reader = command.ExecuteReader())
        {
            var values = new List<decimal>();
            while (reader.Read()) values.Add(decimal.Parse(reader.GetString(0), CultureInfo.InvariantCulture));
            _ordered = values.ToArray();
        }
        command.CommandText = "INSERT INTO decimals(amount) VALUES (NULL); SELECT count(*) FROM decimals WHERE amount IS NULL AND ordering IS NULL";
        _invalidKeys = (long)command.ExecuteScalar()!;
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_sort_numerically_in_sql() => _ordered.ShouldEqual(_values.Concat([0m, 0m, 0m]).Order().ToArray());
    [Fact] void should_match_the_dotnet_encoder_for_every_value() => _sqlKeys.ShouldEqual(_expectedKeys);
    [Fact] void should_normalize_signed_and_scaled_zero() => _zeroKeys.ShouldEqual(Enumerable.Repeat(SqliteDecimalSortKey.For(0m), 3).ToArray());
    [Fact] void should_normalize_equal_scales() => SqliteDecimalSortKey.For(193.58m).ShouldEqual(SqliteDecimalSortKey.For(193.580m));
    [Fact] void should_have_fixed_width_ascii_keys() => _expectedKeys.All(key => key.Length == 58 && key.All(char.IsAscii)).ShouldBeTrue();
    [Fact] void should_preserve_null() => _invalidKeys.ShouldEqual(1);
    [Fact] void should_sort_numerically_in_dotnet() => _values.OrderBy(SqliteDecimalSortKey.For, StringComparer.Ordinal).ShouldEqual(_values.Order());
}
