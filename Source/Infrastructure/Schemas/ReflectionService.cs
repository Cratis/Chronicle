// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Concepts;
using Namotion.Reflection;
using Newtonsoft.Json;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IReflectionService"/> for supporting correct type description creation.
/// </summary>
public class ReflectionService : DefaultReflectionService
{
    /// <inheritdoc/>
    public override bool IsStringEnum(ContextualType contextualType, JsonSerializerSettings serializerSettings) => false;

    /// <inheritdoc/>
    public override JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings)
    {
        if (contextualType.Type.IsConcept())
        {
            defaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            var conceptValueType = contextualType.Type.GetConceptValueType();
            if (contextualType.OriginalNullability == Nullability.Nullable)
            {
                conceptValueType = typeof(Nullable<>).MakeGenericType(conceptValueType);
            }

            contextualType = conceptValueType.ToContextualType(contextualType.Type.GetCustomAttributes());
        }

        return base.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings);
    }
}
