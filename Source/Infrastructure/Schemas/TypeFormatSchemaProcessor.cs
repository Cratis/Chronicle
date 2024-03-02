// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema.Generation;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaProcessor"/> for adding format information.
/// </summary>
public class TypeFormatSchemaProcessor : ISchemaProcessor
{
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeFormatSchemaProcessor"/> class.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving type formats.</param>
    public TypeFormatSchemaProcessor(ITypeFormats typeFormats) => _typeFormats = typeFormats;

    /// <inheritdoc/>
    public void Process(SchemaProcessorContext context)
    {
        if (!_typeFormats.IsKnown(context.ContextualType.Type)) return;

        context.Schema.Format = _typeFormats.GetFormatForType(context.ContextualType.Type);

        if (context.ContextualType.Attributes.OfType<NullableAttribute>().Any() ||
            (context.ContextualType.Parent is not null &&
            context.ContextualType.Type == context.ContextualType.Parent.Type &&
            context.ContextualType.Parent.IsNullableType))
        {
            context.Schema.Format = $"{context.Schema.Format}?";
        }
    }
}
