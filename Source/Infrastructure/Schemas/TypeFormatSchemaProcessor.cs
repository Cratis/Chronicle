// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema.Generation;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaProcessor"/> for adding format information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeFormatSchemaProcessor"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving type formats.</param>
public class TypeFormatSchemaProcessor(ITypeFormats typeFormats) : ISchemaProcessor
{
    /// <inheritdoc/>
    public void Process(SchemaProcessorContext context)
    {
        if (!typeFormats.IsKnown(context.ContextualType.Type)) return;

        context.Schema.Format = typeFormats.GetFormatForType(context.ContextualType.Type);

        if (context.ContextualType.Attributes.OfType<NullableAttribute>().Any() ||
            (context.ContextualType.Parent is not null &&
            context.ContextualType.Type == context.ContextualType.Parent.Type &&
            context.ContextualType.Parent.IsNullableType))
        {
            context.Schema.Format = $"{context.Schema.Format}?";
        }
    }
}
