// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Knows how the string values of a schema are converted, memoized per schema as it is asked for every string converted.
/// </summary>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
class StringConversionKinds(ITypeFormats typeFormats)
{
    static readonly object _true = true;
    static readonly object _false = false;
    readonly ConditionalWeakTable<JsonSchema, object> _writesStringsAsTheyAre = new();
    readonly ConditionalWeakTable<JsonSchema, object> _readsStringsAsTheyAre = new();
    readonly ConditionalWeakTable<JsonSchema, object> _scalarKinds = new();

    /// <summary>
    /// Gets whether a string value is written to JSON exactly as it is when converted against a schema, which holds
    /// for most strings of a state and spares them a node of their own.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the string is written as it is.</returns>
    public bool WritesStringsAsTheyAre(JsonSchema schema) => Memoize(_writesStringsAsTheyAre, schema, ComputeWritesStringsAsTheyAre);

    /// <summary>
    /// Gets whether a string token is read as the string it is when converted against a schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the string is read as it is.</returns>
    public bool ReadsStringsAsTheyAre(JsonSchema schema) => Memoize(_readsStringsAsTheyAre, schema, ComputeReadsStringsAsTheyAre);

    /// <summary>
    /// Gets which scalar a string token of a schema is read directly into, because it is formatted as one.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="StringScalar"/>.</returns>
    public StringScalar ScalarKindOf(JsonSchema schema)
    {
        if (_scalarKinds.TryGetValue(schema, out var known))
        {
            return (StringScalar)known;
        }

        var kind = ComputeScalarKind(schema);
        _scalarKinds.TryAdd(schema, kind);
        return kind;
    }

    static bool Memoize(ConditionalWeakTable<JsonSchema, object> table, JsonSchema schema, Func<JsonSchema, bool> compute)
    {
        if (table.TryGetValue(schema, out var known))
        {
            return ReferenceEquals(known, _true);
        }

        var answer = compute(schema);
        table.TryAdd(schema, answer ? _true : _false);
        return answer;
    }

    bool ComputeWritesStringsAsTheyAre(JsonSchema schema)
    {
        // These are the branches of ConvertToJsonNode that keep a string as it is, in the order it tries them.
        if (ConverterSchemas.HasProtectedValueMetadata(schema) || schema.IsDictionary || schema.Type.HasFlag(JsonObjectType.Array))
        {
            return true;
        }

        if (typeFormats.IsKnown(ConverterSchemas.FormatOf(schema)))
        {
            return false;
        }

        return ConverterSchemas.EffectiveType(schema) is JsonObjectType.String or JsonObjectType.None;
    }

    bool ComputeReadsStringsAsTheyAre(JsonSchema schema) =>
        !schema.IsDictionary &&
        !typeFormats.IsKnown(ConverterSchemas.FormatOf(schema)) &&
        ConverterSchemas.EffectiveType(schema) == JsonObjectType.String &&
        schema.Format != "guid" &&
        schema.OneOf.Count == 0;

    StringScalar ComputeScalarKind(JsonSchema schema)
    {
        var format = ConverterSchemas.FormatOf(schema);
        if (schema.IsDictionary || !typeFormats.IsKnown(format))
        {
            return StringScalar.None;
        }

        var type = typeFormats.GetTypeForFormat(format);
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(Guid))
        {
            return StringScalar.Guid;
        }

        return type == typeof(DateTime) ? StringScalar.DateTime : StringScalar.None;
    }
}
