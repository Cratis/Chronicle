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
/// Represents a custom Orleans serializer for ConceptAs types.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class ConceptAsSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is null)
        {
            return null;
        }

        // ConceptAs types are records with a Value property
        // Since they're immutable, we can return the same instance
        return input;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => IsTypeAllowed(type) ?? false;

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (IsConceptAsType(type))
        {
            return true;
        }

        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var type = field.FieldType;
        var underlyingType = GetUnderlyingType(type);

        // Read the underlying value
        var valueField = reader.ReadFieldHeader();
        var codec = codecProvider.GetCodec(underlyingType);
        var value = codec.ReadValue(ref reader, valueField);

        // Create instance of the ConceptAs type with the value
        return Activator.CreateInstance(type, value)!;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is null)
        {
            return;
        }

        var type = value.GetType();
        var underlyingType = GetUnderlyingType(type);

        // Get the Value property
        var valueProperty = type.GetProperty("Value") ?? throw new InvalidOperationException($"ConceptAs type {type} must have a Value property");
        var underlyingValue = valueProperty.GetValue(value);

        // Write the underlying value
        var codec = codecProvider.GetCodec(underlyingType);
        codec.WriteField(ref writer, fieldIdDelta, underlyingType, underlyingValue!);
    }

    static bool IsConceptAsType(Type type)
    {
        if ((type == null) || (!type.IsClass && !type.IsValueType))
        {
            return false;
        }

        // Check if the type has ConceptAs in its base type hierarchy
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.Name.StartsWith("ConceptAs"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    static Type GetUnderlyingType(Type conceptAsType)
    {
        // Walk up the type hierarchy to find ConceptAs<T>
        var baseType = conceptAsType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.Name.StartsWith("ConceptAs"))
            {
                return baseType.GetGenericArguments()[0];
            }
            baseType = baseType.BaseType;
        }

        throw new InvalidOperationException($"Could not determine underlying type for {conceptAsType}");
    }
}
