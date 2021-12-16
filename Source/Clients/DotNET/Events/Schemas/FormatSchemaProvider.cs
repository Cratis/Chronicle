// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents a <see cref="JSchemaGenerationProvider"/> for adding additional format information.
    /// </summary>
    public class FormatSchemaGenerationProvider : JSchemaGenerationProvider
    {
        /// <inheritdoc/>
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            return context.GetFormatSchemaFor(context.ObjectType);
        }
    }
}
