// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a custom Orleans serializer for <see cref="IEnumerable{T}"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class IEnumerableSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is null)
        {
            return null;
        }

        var inputType = input.GetType();
        if (!IsIEnumerableType(inputType))
        {
            return input;
        }

        // Get the element type
        var elementType = GetElementType(inputType);
        if (elementType is null)
        {
            return input;
        }

        // Convert to list to create a copy
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add")!;

        foreach (var item in (System.Collections.IEnumerable)input)
        {
            addMethod.Invoke(list, [item]);
        }

        return list;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => IsTypeAllowed(type) ?? false;

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (IsIEnumerableType(type))
        {
            return true;
        }
        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var elementType = GetElementType(field.FieldType) ?? throw new InvalidOperationException($"Cannot determine element type for {field.FieldType}");

        // Read the count first
        var countField = reader.ReadFieldHeader();
        var count = codecProvider.GetCodec<int>().ReadValue(ref reader, countField);

        // Create a list to hold the items
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

        // Read each element
        var elementCodec = codecProvider.GetCodec(elementType);
        for (var i = 0; i < count; i++)
        {
            var itemField = reader.ReadFieldHeader();
            var item = elementCodec.ReadValue(ref reader, itemField);
            list.Add(item);
        }

        return list;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is null)
        {
            return;
        }

        var enumerable = (System.Collections.IEnumerable)value;
        var elementType = GetElementType(value.GetType()) ?? throw new InvalidOperationException($"Cannot determine element type for {value.GetType()}");

        // Convert to list to get count
        var items = new List<object?>();
        foreach (var item in enumerable)
        {
            items.Add(item);
        }

        // Write the count
        codecProvider.GetCodec<int>().WriteField(ref writer, fieldIdDelta, typeof(int), items.Count);

        // Write each element
        var elementCodec = codecProvider.GetCodec(elementType);
        foreach (var item in items)
        {
            if (item is not null)
            {
                elementCodec.WriteField(ref writer, fieldIdDelta, elementType, item);
            }
        }
    }

    static bool IsIEnumerableType(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericTypeDef = type.GetGenericTypeDefinition();
        return genericTypeDef == typeof(IEnumerable<>);
    }

    static Type? GetElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }
}
