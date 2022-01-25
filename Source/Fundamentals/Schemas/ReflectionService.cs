// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;
using Namotion.Reflection;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="IReflectionService"/> for supporting correct type description creation.
    /// </summary>
    public class ReflectionService : DefaultReflectionService
    {
        /// <inheritdoc/>
        public override JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings)
        {
            if (contextualType.Type.IsConcept())
            {
                defaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
                contextualType = contextualType.Type.GetConceptValueType().ToContextualType();
            }

            return base.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings);
        }
    }
}
