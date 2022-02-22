// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema.Generation;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaProcessor"/> for adding format information.
/// </summary>
public class TypeFormatSchemaProcessor : ISchemaProcessor
{
    readonly Dictionary<Type, string> _typesFormatInfo = new()
    {
        { typeof(int), "int32" },
        { typeof(uint), "uint32" },
        { typeof(long), "int64" },
        { typeof(ulong), "uint64" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(byte), "byte" },
        { typeof(DateTime), "date-time" },
        { typeof(DateTimeOffset), "date-time" },
    };

    /// <inheritdoc/>
    public void Process(SchemaProcessorContext context)
    {
        if (!_typesFormatInfo.ContainsKey(context.Type)) return;

        context.Schema.Format = _typesFormatInfo[context.Type];
    }
}
