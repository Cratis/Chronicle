// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents an implementation of <see cref="IGeneralizedCodec"/> for <see cref="IEnumerable{T}"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class EnumerableCodec(ICodecProvider codecProvider) : IGeneralizedCodec
{
    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.WireType == WireType.Reference)
        {
            return ReferenceCodec.ReadReference<TInput[], TInput>(ref reader, field);
        }

        var itemCount = reader.ReadInt32();  // Assume we store count first
        var itemType = field.FieldType.GetGenericArguments()[0];
        var codec = codecProvider.GetCodec(itemType);
        var list = (Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) as IList)!;

        for (var i = 0; i < itemCount; i++)
        {
            var item = codec.ReadValue(ref reader, new Field(default, 0, itemType));  // Assume we store field id first
            list.Add(item);
        }

        return list;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        var enumerable = (IEnumerable)value;
        var itemType = expectedType.GetGenericArguments()[0];

        writer.WriteInt32(enumerable.Cast<object>().Count());  // Write count first for deserialization

        var codec = codecProvider.GetCodec(itemType);

        foreach (var item in enumerable)
        {
            codec.WriteField(ref writer, 0, itemType, item);
        }
    }
}
