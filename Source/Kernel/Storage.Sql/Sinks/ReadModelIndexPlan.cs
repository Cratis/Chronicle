// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// A complete provider-specific index plan.
/// </summary>
/// <param name="Definition">The complete index declaration.</param>
/// <param name="Targets">The ordered resolved properties.</param>
internal sealed record ReadModelIndexPlan(IndexDefinition Definition, IReadOnlyList<ReadModelIndexTarget> Targets)
{
    static readonly JsonSerializerOptions _pathOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Gets whether to use a whole-column GIN index.
    /// </summary>
    internal bool IsGin => Targets.Any(target => target.IsArray);

    /// <summary>
    /// Gets whether the definition uses an unchanged single ascending column name.
    /// </summary>
    internal bool IsLegacy => Targets.Count == 1 && !Targets[0].IsExpression && !Targets[0].IsArray && !Targets[0].Descending;

    /// <summary>
    /// Gets the complete expression and direction signature.
    /// </summary>
    internal string Signature => IsGin ? $"gin:{Targets[0].Column.Name}:jsonb_path_ops" : Definition.Signature + JsonSerializer.Serialize(Targets.Select(target => new { target.Column.Name, target.JsonSegments, target.Type }));

    /// <summary>
    /// Gets the deterministic provider index name.
    /// </summary>
    /// <param name="type">The database provider.</param>
    /// <param name="table">The table name.</param>
    /// <returns>The index name.</returns>
    internal string Name(DatabaseType type, string table) => IsLegacy
        ? ReadModelIndexNames.For(type, table, Targets[0].Column.Name)
        : ReadModelIndexNames.ForSignature(type, table, Signature);

    /// <summary>
    /// Resolves every declared property or rejects the complete definition.
    /// </summary>
    /// <param name="definition">The index declaration.</param>
    /// <param name="schema">The read-model schema.</param>
    /// <param name="columns">The projected columns.</param>
    /// <param name="type">The database provider.</param>
    /// <returns>The complete plan, or null when unsupported.</returns>
    internal static ReadModelIndexPlan? Resolve(IndexDefinition definition, JsonSchema schema, IReadOnlyList<ProjectedColumn> columns, DatabaseType type)
    {
        var targets = definition.Properties.Select(property => ReadModelIndexTarget.Resolve(property, schema, columns)).ToArray();
        if (targets.Any(target => target is null)) return null;
        var resolved = targets.OfType<ReadModelIndexTarget>().ToArray();
        if (resolved.Any(target => target.IsArray) && (type != DatabaseType.PostgreSql || resolved.Length != 1 || resolved[0].Descending)) return null;
        return new(definition, resolved);
    }

    /// <summary>
    /// Formats the ordered provider index keys.
    /// </summary>
    /// <param name="type">The database provider.</param>
    /// <param name="helper">The provider SQL generation helper.</param>
    /// <returns>The SQL key list.</returns>
    internal string Keys(DatabaseType type, ISqlGenerationHelper helper) => string.Join(", ", Targets.Select(target =>
    {
        var expression = Expression(target, type, helper);
        return IsGin ? $"{expression} jsonb_path_ops" : $"{expression}{(target.Descending ? " DESC" : " ASC")}";
    }));

    /// <summary>
    /// Formats one provider key expression.
    /// </summary>
    /// <param name="target">The resolved property.</param>
    /// <param name="type">The database provider.</param>
    /// <param name="helper">The provider SQL generation helper.</param>
    /// <returns>The SQL key expression.</returns>
    internal static string Expression(ReadModelIndexTarget target, DatabaseType type, ISqlGenerationHelper helper)
    {
        var column = helper.DelimitIdentifier(target.Column.Name);
        if (!target.IsExpression) return column;
        if (type == DatabaseType.SqlServer) return helper.DelimitIdentifier(SqlServerIndexColumns.Name(target));
        if (type == DatabaseType.Sqlite) return $"json_extract({column}, {Literal(JsonPath(target))})";
        var path = $"ARRAY[{string.Join(", ", target.JsonSegments.Select(Literal))}]::text[]";
        if (target.IsArray) return $"({column} #> {path})";
        var extract = $"({column} #>> {path})";
        var cast = target.Type switch
        {
            JsonObjectType.Integer => "bigint",
            JsonObjectType.Number => "numeric",
            JsonObjectType.Boolean => "boolean",
            _ => null
        };
        return cast is null ? extract : $"({extract}::{cast})";
    }

    /// <summary>
    /// Builds the bounded non-persisted computed expression.
    /// </summary>
    /// <param name="target">The resolved property.</param>
    /// <param name="helper">The provider SQL generation helper.</param>
    /// <returns>The SQL expression.</returns>
    internal static string SqlServerExpression(ReadModelIndexTarget target, ISqlGenerationHelper helper) =>
        $"CAST(JSON_VALUE({helper.DelimitIdentifier(target.Column.Name)}, N{Literal(JsonPath(target))}) AS nvarchar(450))";

    static string JsonPath(ReadModelIndexTarget target) => "$" + string.Concat(target.JsonSegments.Select(segment => "." + JsonSerializer.Serialize(segment, _pathOptions)));
    static string Literal(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
