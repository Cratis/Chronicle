// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reflection;
using OneOf;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a serializer for <see cref="OneOf"/> types and <c>OneOfBase</c> derivatives such as <c>Result&lt;,&gt;</c>.
/// </summary>
/// <remarks>
/// A single generalized codec serves every OneOf type, so the read path receives no expected-type context.
/// The wire format is therefore self-describing: the concrete OneOf type (field 0), the active case index
/// (field 1) and the active value (field 2). The active value is written by its own codec, so concepts and
/// other custom types nested inside a OneOf round-trip correctly across silos. This path is only exercised
/// on cross-silo grain calls — local calls deep-copy the instance directly.
/// </remarks>
public class OneOfSerializer : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    static readonly Type _codecType = typeof(OneOfSerializer);

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => IsTypeAllowed(type) ?? false;

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == _codecType)
        {
            return true;
        }

        if (type.Namespace?.Equals(typeof(OneOf<,>).Namespace) == true)
        {
            return true;
        }

        var current = type;
        while (current != typeof(object) && current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.Contains("OneOfBase"))
            {
                return true;
            }
            current = current.BaseType;
        }

        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.WireType == WireType.Reference)
        {
            return ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
        }

        field.EnsureWireTypeTagDelimited();
        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);

        Type? oneOfType = null;
        var index = 0;
        object? value = null;
        var fieldId = 0u;

        while (true)
        {
            var header = reader.ReadFieldHeader();

            if (header.IsEndBaseOrEndObject)
            {
                break;
            }

            fieldId += header.FieldIdDelta;
            switch (fieldId)
            {
                case 0:
                    oneOfType = TypeSerializerCodec.ReadValue(ref reader, header);
                    break;
                case 1:
                    index = Int32Codec.ReadValue(ref reader, header);
                    break;
                case 2:
                    value = ObjectCodec.ReadValue(ref reader, header);
                    break;
                default:
                    reader.ConsumeUnknownField(header);
                    break;
            }
        }

        var result = Reconstruct(oneOfType!, index, value!);
        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
        return result;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        var oneOf = (IOneOf)value;

        writer.WriteFieldHeader(fieldIdDelta, expectedType, _codecType, WireType.TagDelimited);
        TypeSerializerCodec.WriteField(ref writer, 0, value.GetType());
        Int32Codec.WriteField(ref writer, 1, oneOf.Index);
        ObjectCodec.WriteField(ref writer, 1, oneOf.Value);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reconstructs a OneOf (or OneOfBase derivative) instance from its concrete type, active case index and value.
    /// </summary>
    /// <param name="oneOfType">The concrete OneOf type that was serialized.</param>
    /// <param name="index">The active case index.</param>
    /// <param name="value">The active value.</param>
    /// <returns>The reconstructed instance.</returns>
    /// <exception cref="CannotReconstructOneOf">Thrown when the type cannot be reconstructed.</exception>
    static object Reconstruct(Type oneOfType, int index, object value)
    {
        var oneOfBaseType = GetOneOfTypeWithCases(oneOfType);
        var caseTypes = oneOfBaseType.GetGenericArguments();
        var oneOfStructType = Type.GetType($"OneOf.OneOf`{caseTypes.Length}, OneOf")!.MakeGenericType(caseTypes);
        var fromMethod = oneOfStructType.GetMethod($"FromT{index}", BindingFlags.Public | BindingFlags.Static)!;
        var oneOfStruct = fromMethod.Invoke(null, [value]);

        if (oneOfType == oneOfStructType)
        {
            return oneOfStruct!;
        }

        // A OneOfBase derivative (e.g. Result<,>) — invoke the constructor that takes the OneOf<...> struct.
        var constructor = oneOfType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [oneOfStructType],
            modifiers: null)
            ?? throw new CannotReconstructOneOf(oneOfType);

        return constructor.Invoke([oneOfStruct]);
    }

    /// <summary>
    /// Gets the type that carries the OneOf case type arguments — either the OneOf struct itself or the
    /// <c>OneOfBase&lt;...&gt;</c> base of a derivative.
    /// </summary>
    /// <param name="type">The concrete type.</param>
    /// <returns>The generic type carrying the case type arguments.</returns>
    /// <exception cref="CannotReconstructOneOf">Thrown when the type is not a OneOf or OneOfBase derivative.</exception>
    static Type GetOneOfTypeWithCases(Type type)
    {
        if (type.IsGenericType && type.Namespace == typeof(OneOf<,>).Namespace && type.Name.StartsWith("OneOf`", StringComparison.Ordinal))
        {
            return type;
        }

        var current = type;
        while (current != typeof(object) && current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("OneOfBase`", StringComparison.Ordinal))
            {
                return current;
            }
            current = current.BaseType;
        }

        throw new CannotReconstructOneOf(type);
    }
}
