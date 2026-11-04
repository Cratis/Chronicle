// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents the SQL implementation of <see cref="IReservedEventSequenceIds"/>.
/// </summary>
public class SqlReservedEventSequenceIds : IReservedEventSequenceIds
{
    /// <summary>Created by EF Core for migrations (see SqlDatabaseCatalog.MigrationHistory).</summary>
    const string MigrationsHistoryTable = "__EFMigrationsHistory";

    /// <summary>Derived per event sequence by UniqueConstraintsStorage ("{eventSequenceId}_{constraint}_constraint").</summary>
    const string ConstraintSuffix = "_constraint";

    /// <summary>Derived per event sequence by UniqueConstraintsStorage ("{eventSequenceId}_{constraint}_constraint_values").</summary>
    const string ConstraintValuesSuffix = "_constraint_values";

    static readonly HashSet<string> _names = new(
        typeof(WellKnownTableNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _.IsLiteral && _.FieldType == typeof(string))
            .Select(_ => (string)_.GetRawConstantValue()!)
            .Append(EventSequenceMigrator.NamedTagsTable)
            .Append(MigrationsHistoryTable),
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool IsReserved(EventSequenceId eventSequenceId)
    {
        var value = eventSequenceId.Value;
        return _names.Contains(value) ||
            value.EndsWith(ConstraintSuffix, StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(ConstraintValuesSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
