// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IReflectionService"/>.
/// </summary>
/// <param name="existing">The existing <see cref="IReflectionService"/> that it will forward to for what it does not do itself.</param>
public class ReflectionService(IReflectionService existing) : IReflectionService
{
    /// <inheritdoc/>
    public void GenerateProperties(JsonSchema schema, ContextualType contextualType, JsonSchemaGeneratorSettings settings, NJsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver) =>
        existing.GenerateProperties(schema, contextualType, settings, schemaGenerator, schemaResolver);

    /// <inheritdoc/>
    public JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings)
    {
        if (contextualType.Type.IsConcept())
        {
            defaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            var conceptValueType = contextualType.Type.GetConceptValueType();
            var attributes = contextualType.GetContextAttributes(false).ToList();
            if (contextualType.OriginalNullability == Nullability.Nullable)
            {
                attributes.Add(new NullableAttribute());
            }

            contextualType = conceptValueType.ToContextualType(attributes);
        }

        return existing.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings);
    }

    /// <inheritdoc/>
    public JsonTypeDescription GetDescription(ContextualType contextualType, JsonSchemaGeneratorSettings settings)
    {
        if (contextualType.Type.IsConcept())
        {
            var conceptValueType = contextualType.Type.GetConceptValueType();
            var attributes = contextualType.GetContextAttributes(false).ToList();
            if (contextualType.OriginalNullability == Nullability.Nullable)
            {
                attributes.Add(new NullableAttribute());
            }

            contextualType = conceptValueType.ToContextualType(attributes);
        }

        return existing.GetDescription(contextualType, settings);
    }

    /// <inheritdoc/>
    public Func<object, string?> GetEnumValueConverter(JsonSchemaGeneratorSettings settings) => existing.GetEnumValueConverter(settings);

    /// <inheritdoc/>
    public string GetPropertyName(ContextualAccessorInfo accessorInfo, JsonSchemaGeneratorSettings settings) => existing.GetPropertyName(accessorInfo, settings);

    /// <inheritdoc/>
    public bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling) => existing.IsNullable(contextualType, defaultReferenceTypeNullHandling);

    /// <inheritdoc/>
    public bool IsStringEnum(ContextualType contextualType, JsonSchemaGeneratorSettings settings) => false;
}
