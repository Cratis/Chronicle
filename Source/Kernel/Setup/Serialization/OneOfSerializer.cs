// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using OneOf;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a serializer for <see cref="OneOf"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class OneOfSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => IsTypeAllowed(type) ?? false;

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        // Check if type is in OneOf namespace
        if (type.Namespace?.Equals(typeof(OneOf<,>).Namespace) == true)
        {
            return true;
        }

        // Check if type inherits from OneOfBase
        var current = type;
        while (current != typeof(object) && current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.Contains("OneOfBase"))
            {
                return true;
            }
            current = current.BaseType!;
        }

        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field) => codecProvider.GetCodec(field.FieldType).ReadValue(ref reader, field);

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte> =>
            codecProvider.GetCodec(value.GetType()).WriteField(ref writer, fieldIdDelta, expectedType, value);
}
