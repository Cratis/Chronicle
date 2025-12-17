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
/// Represents a serializer for working with concepts for Orleans.
/// </summary>
public class ConceptSerializer : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type.IsConcept();

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type.IsConcept())
        {
            return true;
        }

        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var targetType = typeof(TInput).GetConceptValueType();
        object value = null!;

        if (targetType.IsEnum)
        {
            value = Int32Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(string))
        {
            value = StringCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(byte))
        {
            value = ByteCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(char))
        {
            value = CharCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(short))
        {
            value = Int16Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(int))
        {
            value = Int32Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(long))
        {
            value = Int64Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(ushort))
        {
            value = UInt16Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(uint))
        {
            value = UInt32Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(ulong))
        {
            value = UInt64Codec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(float))
        {
            value = FloatCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(double))
        {
            value = DoubleCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(decimal))
        {
            value = DecimalCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(TimeOnly))
        {
            value = TimeOnlyCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(DateOnly))
        {
            value = DateOnlyCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(DateTime))
        {
            value = DateTimeCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(DateTimeOffset))
        {
            value = DateTimeOffsetCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(Guid))
        {
            value = GuidCodec.ReadValue(ref reader, field);
        }
        else if (field.FieldType == typeof(bool))
        {
            value = BoolCodec.ReadValue(ref reader, field);
        }

        if (value is null)
        {
            return null!;
        }

        return ConceptFactory.CreateConceptInstance(targetType, value);
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var type = value.GetType().GetConceptValueType();
        value = value.GetConceptValue();

        if (type.IsEnum)
        {
            Int32Codec.WriteField(ref writer, fieldIdDelta, (int)value);
            return;
        }

        switch (value)
        {
            case string stringValue:
                StringCodec.WriteField(ref writer, fieldIdDelta, stringValue);
                break;
            case byte byteValue:
                ByteCodec.WriteField(ref writer, fieldIdDelta, byteValue);
                break;
            case char charValue:
                CharCodec.WriteField(ref writer, fieldIdDelta, charValue);
                break;
            case short shortValue:
                Int16Codec.WriteField(ref writer, fieldIdDelta, shortValue);
                break;
            case int intValue:
                Int32Codec.WriteField(ref writer, fieldIdDelta, intValue);
                break;
            case long longValue:
                Int64Codec.WriteField(ref writer, fieldIdDelta, longValue);
                break;
            case ushort shortValue:
                UInt16Codec.WriteField(ref writer, fieldIdDelta, shortValue);
                break;
            case uint intValue:
                UInt32Codec.WriteField(ref writer, fieldIdDelta, intValue);
                break;
            case ulong longValue:
                UInt64Codec.WriteField(ref writer, fieldIdDelta, longValue);
                break;
            case float floatValue:
                FloatCodec.WriteField(ref writer, fieldIdDelta, floatValue);
                break;
            case double doubleValue:
                DoubleCodec.WriteField(ref writer, fieldIdDelta, doubleValue);
                break;
            case decimal decimalValue:
                DecimalCodec.WriteField(ref writer, fieldIdDelta, decimalValue);
                break;
            case TimeOnly timeOnlyValue:
                TimeOnlyCodec.WriteField(ref writer, fieldIdDelta, timeOnlyValue);
                break;
            case DateOnly dateOnlyValue:
                DateOnlyCodec.WriteField(ref writer, fieldIdDelta, dateOnlyValue);
                break;
            case DateTime dateTimeValue:
                DateTimeCodec.WriteField(ref writer, fieldIdDelta, dateTimeValue);
                break;
            case DateTimeOffset dateTimeOffsetValue:
                DateTimeOffsetCodec.WriteField(ref writer, fieldIdDelta, dateTimeOffsetValue);
                break;
            case Guid guidValue:
                GuidCodec.WriteField(ref writer, fieldIdDelta, guidValue);
                break;
            case bool boolValue:
                BoolCodec.WriteField(ref writer, fieldIdDelta, boolValue);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
