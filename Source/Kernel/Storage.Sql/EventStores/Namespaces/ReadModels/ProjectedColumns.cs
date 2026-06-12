// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Translates a read model's <see cref="JsonSchema"/> into the list of <see cref="ProjectedColumn"/> values
/// that <see cref="ReadModelDbContext"/> registers on EF and <see cref="ReadModelMigrator"/> creates as
/// real SQL columns. Leaf properties become typed columns; arrays and complex objects become JSON columns.
/// </summary>
public static class ProjectedColumns
{
    static readonly TypeFormats _typeFormats = new();

    /// <summary>
    /// Derive the table columns for a read model from its latest-generation <see cref="JsonSchema"/>.
    /// The returned list always includes <c>Id</c> as the primary key column (using the schema's likely
    /// key name when it does not declare an explicit <c>Id</c> / <c>id</c>) and the sink-owned
    /// <see cref="WellKnownProperties.LastHandledEventSequenceNumber"/> bookkeeping column.
    /// </summary>
    /// <param name="schema">The read model's schema.</param>
    /// <returns>The ordered list of columns.</returns>
    public static IReadOnlyList<ProjectedColumn> ForSchema(JsonSchema schema)
    {
        var columns = new List<ProjectedColumn>();

        string keyName;
        if (schema.HasKeyProperty())
        {
            keyName = schema.Properties.ContainsKey("Id") ? "Id" : "id";
        }
        else
        {
            keyName = schema.GetLikelyKeyPropertyName() ?? "Id";
        }

        var schemaHasKey = schema.Properties.ContainsKey(keyName);

        // The schema's own properties — including the key when present in the schema. Order them so the
        // primary key appears first; the rest follow in schema declaration order. The sink-owned
        // bookkeeping column is always emitted last; if the schema also declares it (read models that
        // expose __lastHandledEventSequenceNumber publicly do), we skip the schema copy because the
        // bookkeeping declaration here owns the canonical typed-column shape (Int64, nullable).
        var orderedNames = schema.Properties.Keys
            .Where(name => !string.Equals(name, WellKnownProperties.LastHandledEventSequenceNumber, StringComparison.Ordinal))
            .OrderBy(name => string.Equals(name, keyName, StringComparison.Ordinal) ? 0 : 1)
            .ToArray();

        foreach (var name in orderedNames)
        {
            var property = schema.Properties[name];
            var isKey = string.Equals(name, keyName, StringComparison.Ordinal);
            columns.Add(BuildColumn(name, property, isKey));
        }

        // If the schema does not declare an Id, synthesize one. Reads/writes inject the key value
        // via Sink.GetIdString — we still need a real PK column on the table.
        if (!schemaHasKey)
        {
            columns.Insert(0, new ProjectedColumn(keyName, typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false));
        }

        // Sink-owned bookkeeping column. Stored as long? because EventSequenceNumber is a ulong concept
        // and EF requires the CLR type itself to be nullable when the column is nullable. We cap the
        // value at signed 63-bit on read/write (every backend has an INT64 / BIGINT equivalent).
        columns.Add(new ProjectedColumn(
            WellKnownProperties.LastHandledEventSequenceNumber,
            typeof(long?),
            IsKey: false,
            IsJson: false,
            IsArray: false,
            IsNullable: true));

        return columns;
    }

    static ProjectedColumn BuildColumn(string name, JsonSchemaProperty property, bool isKey)
    {
        // The primary-key column always lives in a typed scalar column even when the schema
        // describes a composite key (Object shape) — Sink.GetIdString flattens composite keys
        // to a single deterministic string, which we then store as TEXT. Falling through to the
        // JSON branch would mark the column with the provider's JSON type and (for SQLite) fail a
        // CHECK constraint when the value is not valid JSON.
        if (isKey)
        {
            var keyClrType = property.GetTargetTypeForJsonSchemaProperty(_typeFormats) ?? typeof(string);
            if (IsJsonShape(property))
            {
                keyClrType = typeof(string);
            }

            return new ProjectedColumn(name, keyClrType, IsKey: true, IsJson: false, IsArray: false, IsNullable: false);
        }

        // Every non-key column is stored as nullable. The projection engine treats "not yet
        // projected" as missing, not as a zero/empty value: a property that an older projection
        // definition didn't populate should round-trip as null, not as the type's default (e.g.
        // "" for string). MongoDB's document model gets this for free because BSON simply omits
        // missing fields; we match the behavior by declaring every non-key column nullable so
        // that schema upgrades don't manufacture empty values for rows written before the upgrade.
        if (IsJsonShape(property))
        {
            return new ProjectedColumn(name, typeof(string), IsKey: false, IsJson: true, IsArray: property.IsArray, IsNullable: true);
        }

        var clrType = property.GetTargetTypeForJsonSchemaProperty(_typeFormats) ?? typeof(string);
        var nullableClr = clrType.IsValueType && Nullable.GetUnderlyingType(clrType) is null
            ? typeof(Nullable<>).MakeGenericType(clrType)
            : clrType;
        return new ProjectedColumn(name, nullableClr, IsKey: false, IsJson: false, IsArray: false, IsNullable: true);
    }

    static bool IsJsonShape(JsonSchemaProperty property)
    {
        if (property.IsArray)
        {
            return true;
        }

        // A property with a known primitive format (guid, int32, date-time, …) is a leaf — even when
        // the JSON schema type happens to overlap with Object/None due to concept handling. The format
        // wins because Cratis's schema generator stamps the underlying primitive's format onto the
        // concept schema. Complex known formats (e.g. geospatial types) decompose into a nested JSON
        // value, so they are stored as JSON columns rather than scalar typed columns.
        if (!string.IsNullOrEmpty(property.Format) && _typeFormats.IsKnown(property.Format))
        {
            return _typeFormats.IsComplexFormat(property.Format);
        }

        // Anything that decomposes into nested fields (Type=Object, or a $ref to an object schema)
        // is stored as JSON. We don't want a column-per-leaf-of-a-nested-record on the parent table.
        if (property.Type.HasFlag(JsonObjectType.Object))
        {
            return true;
        }

        if (property.HasReference && property.Reference is not null)
        {
            var referencedType = property.Reference.Type;
            if (referencedType.HasFlag(JsonObjectType.Object) || referencedType.HasFlag(JsonObjectType.Array))
            {
                return true;
            }
        }

        return false;
    }
}
