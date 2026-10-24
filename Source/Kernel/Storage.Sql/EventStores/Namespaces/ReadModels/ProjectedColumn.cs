// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Describes a single column on a read-model table derived from the read model's <see cref="Schemas.JsonSchema"/>.
/// Leaf properties (primitives, dates, guids, concepts of those) map to a typed column. Collections and
/// complex objects map to a single JSON-typed column (jsonb on PostgreSQL, nvarchar(max) on SQL Server, text on SQLite).
/// </summary>
/// <param name="Name">The column name as it appears in the read-model JSON / on the table.</param>
/// <param name="ClrType">The CLR type used by EF for the column. JSON columns use <see cref="string"/>.</param>
/// <param name="IsKey">True when this column is the read model's primary key.</param>
/// <param name="IsJson">True when this column stores a serialized JSON document (collections or nested objects).</param>
/// <param name="IsArray">When <see cref="IsJson"/> is true, indicates the JSON document holds an array (otherwise an object).</param>
/// <param name="IsNullable">Whether the column accepts NULL.</param>
public record ProjectedColumn(string Name, Type ClrType, bool IsKey, bool IsJson, bool IsArray, bool IsNullable)
{
    /// <summary>
    /// Gets the explicitly declared maximum string length.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets whether this column has a declared scalar index.
    /// </summary>
    public bool IsIndexed { get; init; }

    /// <summary>
    /// Gets whether the key schema is object-shaped, while its SQL column remains the legacy scalar key.
    /// </summary>
    internal bool IsCompositeKey { get; init; }

    /// <summary>
    /// Gets the SQL Server string storage type, including scalar-index key sizing.
    /// </summary>
    internal string SqlServerStringType
    {
        get
        {
            if (IsKey) return "NVARCHAR(200)";
            if (!IsIndexed && MaxLength is null) return "NVARCHAR(MAX)";
            return MaxLength switch
            {
                > 0 and <= 850 => $"NVARCHAR({MaxLength})",
                null => "NVARCHAR(450)",
                _ => "NVARCHAR(MAX)"
            };
        }
    }
}
