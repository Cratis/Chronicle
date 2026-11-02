// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Npgsql;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.LegacyIdentifierUpgrade;

public sealed partial class LegacyIdentifierDatabase(SqlClusterDatabase database)
{
    string _connectionString = string.Empty;
    string _databaseName = string.Empty;
    string _schema = string.Empty;

    public sealed record Row(string Id, string? Identifier, string? Name, string Tuple, string Version);
    public sealed record Snapshot(uint Relation, uint File, uint PrimaryIndex, IReadOnlyList<string> Columns, IReadOnlyList<string> Keys, IReadOnlyList<Row> Rows);

    public async Task Discover(CancellationToken token)
    {
        await using var root = (NpgsqlConnection)database.CreateConnection();
        await root.OpenAsync(token);
        var names = new List<string>();
        await using (var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datallowconn AND NOT datistemplate", root))
        await using (var reader = await command.ExecuteReaderAsync(token))
        {
            while (await reader.ReadAsync(token)) names.Add(reader.GetString(0));
        }

        var matches = new List<(string Connection, string Database, string Schema)>();
        foreach (var name in names)
        {
            var builder = new NpgsqlConnectionStringBuilder(root.ConnectionString) { Database = name, ApplicationName = "legacy-key-evidence" };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = new NpgsqlCommand("SELECT table_schema FROM information_schema.tables WHERE table_name = @table AND table_type = 'BASE TABLE'", connection);
            command.Parameters.AddWithValue("table", LegacyIdentifierClient.Table);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) matches.Add((builder.ConnectionString, name, reader.GetString(0)));
        }

        var match = matches.Single();
        _schema = match.Schema;
        _connectionString = new NpgsqlConnectionStringBuilder(match.Connection) { SearchPath = Quote(_schema) }.ConnectionString;
        _databaseName = match.Database;
    }

    public async Task<Snapshot> Capture(CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(token);
        uint relation;
        uint file;
        uint primaryIndex;
        await using (var command = new NpgsqlCommand("SELECT c.oid, c.relfilenode, (SELECT indexrelid FROM pg_index WHERE indrelid = c.oid AND indisprimary) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = @schema AND c.relname = @table AND c.relkind = 'r'", connection))
        {
            command.Parameters.AddWithValue("schema", _schema);
            command.Parameters.AddWithValue("table", LegacyIdentifierClient.Table);
            await using var reader = await command.ExecuteReaderAsync(token);
            (await reader.ReadAsync(token)).ShouldBeTrue();
            relation = await reader.GetFieldValueAsync<uint>(0, token);
            file = await reader.GetFieldValueAsync<uint>(1, token);
            primaryIndex = await reader.GetFieldValueAsync<uint>(2, token);
            (await reader.ReadAsync(token)).ShouldBeFalse();
        }

        var columns = await Strings(connection, keys: false, relation, token);
        var keys = await Strings(connection, keys: true, relation, token);
        var rows = new List<Row>();
        await using (var command = new NpgsqlCommand("SELECT \"Id\", \"ID\", \"Name\", ctid::text, xmin::text FROM \"LegacyIdentifier5512\" ORDER BY \"Id\"", connection))
        await using (var reader = await command.ExecuteReaderAsync(token))
        {
            while (await reader.ReadAsync(token))
            {
                rows.Add(new(reader.GetString(0), await reader.IsDBNullAsync(1, token) ? null : reader.GetString(1), await reader.IsDBNullAsync(2, token) ? null : reader.GetString(2), reader.GetString(3), reader.GetString(4)));
            }
        }

        return new(relation, file, primaryIndex, columns, keys, [.. rows]);
    }

    public static void AssertLegacy(Snapshot snapshot)
    {
        snapshot.Relation.ShouldNotEqual(0u);
        snapshot.File.ShouldNotEqual(0u);
        snapshot.PrimaryIndex.ShouldNotEqual(0u);
        snapshot.Columns.Count.ShouldEqual(3);
        snapshot.Keys.ShouldEqual("Id");
        snapshot.Columns.Single(column => column.StartsWith("ID:", StringComparison.Ordinal)).ShouldEqual("ID:text:false");
        snapshot.Columns.ShouldContain("Id:text:true");
        snapshot.Columns.ShouldContain("Name:text:false");
        snapshot.Rows.Count.ShouldBeGreaterThan(0);
        snapshot.Rows.Select(row => row.Id).Distinct().Count().ShouldEqual(snapshot.Rows.Count);
    }

    public static void AssertRetained(Snapshot before, Snapshot after, bool physical = true)
    {
        AssertLegacy(after);
        ShouldCollectionExtensions.ShouldEqual(after.Columns, before.Columns.ToArray());
        ShouldCollectionExtensions.ShouldEqual(after.Keys, before.Keys.ToArray());
        if (physical)
        {
            after.Relation.ShouldEqual(before.Relation);
            after.File.ShouldEqual(before.File);
            after.PrimaryIndex.ShouldEqual(before.PrimaryIndex);
            after.Rows.Single(row => row.Id == "sentinel").ShouldEqual(before.Rows.Single(row => row.Id == "sentinel"));
        }
    }

    public void AssertTrace(string before, string after, params string[] requiredVerbs)
    {
        // A lost/truncated log or missing activity is not proof that no migration ran.
        after.StartsWith(before, StringComparison.Ordinal).ShouldBeTrue();
        var entries = StatementStart.Split(after[before.Length..]);
        var statements = entries.Where(entry => entry.Contains($"database={_databaseName} ", StringComparison.Ordinal)
                && !entry.Contains("application=legacy-key-evidence ", StringComparison.Ordinal))
            .Select(entry => SqlText.Match(entry).Groups["sql"].Value)
            .Where(sql => sql.Contains($"\"{LegacyIdentifierClient.Table}\"", StringComparison.Ordinal)).ToArray();
        statements.Length.ShouldBeGreaterThan(0);
        foreach (var verb in requiredVerbs)
        {
            statements.Any(sql => sql.TrimStart().StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        }

        foreach (var sql in statements) AssertOrdinaryStatement(sql);
    }

    static void AssertOrdinaryStatement(string sql)
    {
        var ordinary = Ordinary.IsMatch(sql);
        var idempotentCreate = IdempotentCreate.IsMatch(sql) && !AsSelect.IsMatch(sql);
        (ordinary || idempotentCreate).ShouldBeTrue();
        if (Insert.IsMatch(sql))
        {
            ContainsSelect.IsMatch(sql).ShouldBeFalse();
        }
        if (Select.IsMatch(sql))
        {
            ContainsInto.IsMatch(sql).ShouldBeFalse();
        }
        Forbidden.IsMatch(sql).ShouldBeFalse();
        if (Update.IsMatch(sql))
        {
            KeyPredicate.IsMatch(sql).ShouldBeTrue();
            var assignments = Assignments.Match(sql).Groups["set"].Value;
            assignments.Contains("\"Id\"", StringComparison.Ordinal).ShouldBeFalse();
        }
    }

    static async Task<string[]> Strings(NpgsqlConnection connection, bool keys, uint relation, CancellationToken token)
    {
        await using var command = keys
            ? new NpgsqlCommand("SELECT a.attname FROM pg_index i CROSS JOIN LATERAL unnest(i.indkey) WITH ORDINALITY k(attnum, ord) JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = k.attnum WHERE i.indrelid = @relation AND i.indisprimary ORDER BY k.ord", connection)
            : new NpgsqlCommand("SELECT a.attname || ':' || format_type(a.atttypid, a.atttypmod) || ':' || a.attnotnull FROM pg_attribute a WHERE a.attrelid = @relation AND a.attnum > 0 AND NOT a.attisdropped ORDER BY a.attnum", connection);
        command.Parameters.AddWithValue(nameof(relation), relation);
        await using var reader = await command.ExecuteReaderAsync(token);
        var values = new List<string>();
        while (await reader.ReadAsync(token)) values.Add(reader.GetString(0));
        return [.. values];
    }

    static string Quote(string name) => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    [GeneratedRegex(@"(?m)(?=^\d{4}-\d\d-\d\d .+? \[\d+\] database=)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex StatementStart { get; }

    [GeneratedRegex(@"(?:statement:|execute [^:]*:)\s*(?<sql>[\s\S]*)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SqlText { get; }

    [GeneratedRegex(@"^\s*(?:SELECT|INSERT INTO|UPDATE)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Ordinary { get; }

    [GeneratedRegex(@"^\s*CREATE TABLE IF NOT EXISTS\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IdempotentCreate { get; }

    [GeneratedRegex(@"\bAS\s+SELECT\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex AsSelect { get; }

    [GeneratedRegex(@"^\s*INSERT\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Insert { get; }

    [GeneratedRegex(@"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ContainsSelect { get; }

    [GeneratedRegex(@"^\s*SELECT\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Select { get; }

    [GeneratedRegex(@"\bINTO\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ContainsInto { get; }

    [GeneratedRegex(@"\b(?:ALTER|DROP|TRUNCATE|RENAME|COPY|DELETE)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Forbidden { get; }

    [GeneratedRegex(@"^\s*UPDATE\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Update { get; }

    [GeneratedRegex("\\bWHERE\\s+\"Id\"\\s*=", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex KeyPredicate { get; }

    [GeneratedRegex(@"\bSET\b(?<set>[\s\S]*?)\bWHERE\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Assignments { get; }
}
