// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;

/// <summary>
/// Builds single-row, parameterized conditional mutations for each SQL provider.
/// </summary>
public static class AlertIncidentStatements
{
    static readonly string[] _columns = ["Id", "EventStore", "Namespace", "ObserverId", "EventSequenceId", "Partition", "Condition", "Severity", "AttemptCount", "FirstFailure", "LastFailure", "FailureKind", "Message", "RaisedAt", "RaisedSequenceNumber", "LastChangedAt", "LastTransitionSequenceNumber", "IsOpen", "ClearedReason"];

    /// <summary>
    /// Gets the statement and values for an atomic conditional write.
    /// </summary>
    /// <param name="context">The namespace context providing identifier quoting and provider selection.</param>
    /// <param name="row">The incoming values.</param>
    /// <param name="kind">The transition kind.</param>
    /// <returns>The SQL format and its parameter values.</returns>
    public static (string Sql, object[] Values) For(NamespaceDbContext context, AlertIncidentEntity row, AlertIncidentTransitionKind kind)
    {
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier(WellKnownTableNames.AlertIncidents);
        var id = helper.DelimitIdentifier(nameof(row.Id));
        var sequence = helper.DelimitIdentifier(nameof(row.LastTransitionSequenceNumber));
        var open = helper.DelimitIdentifier(nameof(row.IsOpen));
        var columns = _columns.Select(helper.DelimitIdentifier).ToArray();
        var parameters = _columns.Select((_, index) => $"{{{index}}}").ToArray();
        object?[] rawValues = [row.Id, row.EventStore, row.Namespace, row.ObserverId, row.EventSequenceId, row.Partition, row.Condition,
            row.Severity, row.AttemptCount, row.FirstFailure,
            row.LastFailure, row.FailureKind, row.Message,
            row.RaisedAt, row.RaisedSequenceNumber, row.LastChangedAt,
            row.LastTransitionSequenceNumber, row.IsOpen, row.ClearedReason];
        DbType[] types = [DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String,
            DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.Int32, DbType.String, DbType.String,
            DbType.Int64, DbType.String, DbType.Int64, DbType.Boolean, DbType.Int32];
        using var command = context.Database.GetDbConnection().CreateCommand();
        var values = rawValues.Select((value, index) =>
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"p{index}";
            parameter.DbType = types[index];
            parameter.Value = value ?? DBNull.Value;

            return (object)parameter;
        }).ToArray();
        var indexes = kind switch
        {
            AlertIncidentTransitionKind.Raised => Enumerable.Range(1, _columns.Length - 1),
            AlertIncidentTransitionKind.Escalated => [6, 7, 8, 9, 10, 11, 12, 15, 16],
            _ => [1, 2, 3, 4, 5, 6, 15, 16, 17, 18]
        };
        if (kind == AlertIncidentTransitionKind.Escalated)
        {
            var assignments = string.Join(", ", indexes.Select(index => $"{columns[index]} = {parameters[index]}"));
            var trueValue = context.Database.IsNpgsql() ? "TRUE" : "1";

            return ($"UPDATE {table} SET {assignments} WHERE {id} = {{0}} AND {open} = {trueValue} AND {sequence} < {{16}}", values);
        }
        if (context.Database.IsSqlServer())
        {
            var source = string.Join(", ", columns.Select((column, index) => $"{parameters[index]} AS {column}"));
            var assignments = string.Join(", ", indexes.Select(index => $"target.{columns[index]} = source.{columns[index]}"));

            return ($"MERGE {table} WITH (HOLDLOCK) AS target USING (SELECT {source}) AS source ON target.{id} = source.{id} " +
                $"WHEN MATCHED AND target.{sequence} < source.{sequence} THEN UPDATE SET {assignments} " +
                $"WHEN NOT MATCHED THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => $"source.{column}"))});", values);
        }
        var updates = string.Join(", ", indexes.Select(index => $"{columns[index]} = excluded.{columns[index]}"));

        return ($"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)}) " +
            $"ON CONFLICT ({id}) DO UPDATE SET {updates} WHERE {table}.{sequence} < excluded.{sequence}", values);
    }
}
