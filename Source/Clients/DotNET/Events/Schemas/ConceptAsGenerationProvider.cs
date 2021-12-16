// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents a <see cref="JSchemaGenerationProvider"/> for concepts.
    /// </summary>
    public class ConceptAsGenerationProvider : JSchemaGenerationProvider
    {
        /// <inheritdoc/>
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            if (context.ObjectType.IsConcept())
            {
                var conceptType = context.ObjectType.GetConceptValueType();
                var schema = context.GetFormatSchemaFor(conceptType);
                if (schema == default)
                {
                    var generator = new JSchemaGenerator();
                    schema = generator.Generate(conceptType, context.Required != Required.Always);
                }
                return schema;
            }
            return null!;
        }
    }
}
