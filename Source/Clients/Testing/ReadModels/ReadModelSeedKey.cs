// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Dynamic;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using CompositeExpression = KernelCore::Cratis.Chronicle.Projections.Engine.Expressions.Keys.CompositeKeyExpression;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Resolves named seed identities from selected root mappings and the native read model schema.
/// </summary>
/// <param name="modelType">The model type.</param>
/// <param name="schemaGenerator">The fold's schema generator.</param>
/// <param name="parts">The mapped identifier-schema parts.</param>
internal sealed class ReadModelSeedKey(Type modelType, IJsonSchemaGenerator schemaGenerator, IReadOnlyDictionary<string, JsonSchemaProperty> parts)
{
    /// <summary>
    /// Resolves the selected root composite contract.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="projection">The selected projection.</param>
    /// <param name="schemaGenerator">The fold's schema generator.</param>
    /// <returns>The validated contract.</returns>
    /// <exception cref="InvalidReadModelSeedKey">The selected projection has no unambiguous scalar composite contract.</exception>
    public static ReadModelSeedKey Create(Type modelType, ProjectionDefinition? projection, IJsonSchemaGenerator schemaGenerator)
    {
        if (projection is null) throw new InvalidReadModelSeedKey(modelType, "a selected root composite projection is required; reducers and lookup-only seeds do not define composite keys.");
        var expressions = projection.From.Values.Select(from => from.Key).Concat(projection.RemovedWith.Values.Select(removed => removed.Key)).ToArray();
        if (expressions.Length == 0 || expressions.Any(expression => expression?.StartsWith("$composite(", StringComparison.Ordinal) != true))
        {
            throw new InvalidReadModelSeedKey(modelType, "all selected root mappings must use compatible composite key parts.");
        }

        var mapped = expressions.Select(expression => CompositeExpression.Parse(expression).Mappings.Select(mapping => mapping.Property).ToArray()).ToArray();
        var names = mapped[0].ToHashSet(StringComparer.Ordinal);
        if (mapped.Any(parts => !names.SetEquals(parts))) throw new InvalidReadModelSeedKey(modelType, "selected root composite mappings have incompatible part sets.");
        var schema = ProjectionReadModelProcessor.GetReadModelSchema(modelType, schemaGenerator);
        if (!schema.HasKeyProperty()) throw new InvalidReadModelSeedKey(modelType, "the read model has no identifier property.");
        var identifierPath = new PropertyPath(schema.GetKeyProperty().Name);
        var parts = names.ToDictionary(name => name, name => ResolvePart(modelType, schema, identifierPath + name), StringComparer.Ordinal);
        return new(modelType, schemaGenerator, parts);
    }

    /// <summary>
    /// Detaches, validates and normalizes the supplied parts using native sink identity.
    /// </summary>
    /// <param name="supplied">The canonical mapped parts.</param>
    /// <returns>The native key and its sink identity.</returns>
    /// <exception cref="InvalidReadModelSeedKey">The supplied names do not match the contract.</exception>
    public (ExpandoObject Key, EventSourceId Identity) Normalize(IReadOnlyDictionary<string, object?> supplied)
    {
        if (!parts.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(supplied.Keys))
        {
            throw new InvalidReadModelSeedKey(modelType, $"expected exactly canonical mapped parts [{string.Join(", ", parts.Keys)}]; actual parts [{string.Join(", ", supplied.Keys)}]. Names are ordinal and case-sensitive, not CLR aliases.");
        }
        var key = new ExpandoObject();
        var values = (IDictionary<string, object?>)key;
        foreach (var (name, schema) in parts)
        {
            values.Add(name, new ReadModelSeedKeyValues(modelType, new(name), schema).Convert(supplied[name]));
        }
        using var sink = ProjectionReadModelProcessor.CreateSink(modelType, schemaGenerator);
        return (key, new EventSourceId((string)sink.GetKeyValue(new KernelKey(key, ArrayIndexers.NoIndexers))));
    }

    static JsonSchemaProperty ResolvePart(Type modelType, JsonSchema schema, PropertyPath path)
    {
        if (path.Segments.Count() != 2) throw InvalidPath(modelType, path);
        var identifier = ResolveProperty(modelType, schema, path, path.Segments.First().Value);
        var part = ResolveProperty(modelType, identifier.ActualTypeSchema, path, path.Segments.Last().Value);
        if (!IsScalar(part)) throw InvalidPath(modelType, path);
        return part;
    }

    static JsonSchemaProperty ResolveProperty(Type modelType, JsonSchema schema, PropertyPath path, string name)
    {
        var matches = schema.GetFlattenedProperties().Where(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) throw InvalidPath(modelType, path);
        return matches[0];
    }

    static bool IsScalar(JsonSchemaProperty property) =>
        property.GetTargetTypeForJsonSchemaProperty(new TypeFormats()) is not null &&
        !property.IsArray && !property.ActualSchema.Type.HasFlag(JsonObjectType.Object);

    static InvalidReadModelSeedKey InvalidPath(Type modelType, PropertyPath path) =>
        new(modelType, $"mapped key part '{path.Segments.Last()}' requires an existing, unambiguous scalar identifier-schema path '{path}'.");
}
