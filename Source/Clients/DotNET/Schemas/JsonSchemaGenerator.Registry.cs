// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaGenerator
{
    readonly ConcurrentDictionary<Type, StructuralSchema> _preciseEventStructures = new();
    readonly ConcurrentDictionary<Type, StructuralSchema> _legacyEventStructures = new();

    /// <inheritdoc/>
    public JsonSchema Generate(Type type, IEventTypes eventTypes) => CompensationSchemaBinding.RequiresRegistry(type, eventTypes)
        ? BindStructure(_preciseEventStructures.GetOrAdd(type, static (clrType, generator) => generator.GenerateStructure(clrType, precise: true), this), eventTypes)
        : Generate(type);

    /// <inheritdoc/>
    public JsonSchema GenerateLegacyEventType(Type type, IEventTypes eventTypes) => CompensationSchemaBinding.RequiresRegistry(type, eventTypes)
        ? BindStructure(_legacyEventStructures.GetOrAdd(type, static (clrType, generator) => generator.GenerateStructure(clrType, precise: false), this), eventTypes)
        : GenerateLegacyEventType(type);

    static JsonSchema BindStructure(StructuralSchema structure, IEventTypes eventTypes)
    {
        var clone = structure.Node.DeepClone();
        Decorate(structure.Node, clone, structure.Compensations, eventTypes);

        return new(clone.AsObject());
    }

    static void Decorate(JsonNode original, JsonNode clone, FrozenDictionary<JsonObject, Type> compensations, IEventTypes eventTypes)
    {
        if (original is JsonObject originalObject && clone is JsonObject cloneObject)
        {
            if (compensations.TryGetValue(originalObject, out var target))
            {
                cloneObject[CompensationJsonSchemaExtensions.CompensationForKey] = CompensationSchemaBinding.Resolve(target, eventTypes).Value;
            }
            foreach (var property in originalObject.Where(_ => _.Value is not null))
            {
                Decorate(property.Value!, cloneObject[property.Key]!, compensations, eventTypes);
            }
        }
        else if (original is JsonArray originalArray && clone is JsonArray cloneArray)
        {
            for (var index = 0; index < originalArray.Count; index++)
            {
                if (originalArray[index] is { } item)
                {
                    Decorate(item, cloneArray[index]!, compensations, eventTypes);
                }
            }
        }
    }

    StructuralSchema GenerateStructure(Type type, bool precise)
    {
        var compensations = new Dictionary<JsonObject, Type>(ReferenceEqualityComparer.Instance);
        var options = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, schema) => TransformNode(context, schema, precise, (node, target) => compensations[node] = target)
        };
        var node = _serializerOptions.GetJsonSchemaAsNode(type, options).AsObject();

        return new(node, compensations.ToFrozenDictionary(ReferenceEqualityComparer.Instance));
    }

    JsonSchemaExporterOptions StructuralOptionsFor(Action<JsonObject, Type>? recordCompensation, bool preserveUnformattedNullability) => recordCompensation is null && !preserveUnformattedNullability
        ? _exporterOptions
        : new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, schema) => TransformNode(context, schema, restorePropertiesWithDefaultValues: false, recordCompensation, preserveUnformattedNullability)
        };

    /// <summary>
    /// CLR-only structure and unresolved compensation locations, never decorated with a store identifier.
    /// </summary>
    /// <param name="Node">The structural schema node.</param>
    /// <param name="Compensations">The unresolved targets on nodes reached by the normal transformation path.</param>
    sealed record StructuralSchema(JsonObject Node, FrozenDictionary<JsonObject, Type> Compensations);
}
