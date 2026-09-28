// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a custom Orleans serializer for <see cref="ExpandoObject"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class ExpandoObjectSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is not ExpandoObject original)
        {
            return input;
        }

        var dictionary = (IDictionary<string, object?>)original;
        var dictionaryCopier = codecProvider.GetDeepCopier<Dictionary<string, object?>>();
        var copiedDictionary = dictionaryCopier.DeepCopy(new Dictionary<string, object?>(dictionary), context);

        var result = new ExpandoObject();
        var resultDict = (IDictionary<string, object?>)result;
        foreach (var kvp in copiedDictionary)
        {
            resultDict[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == typeof(ExpandoObject);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == typeof(ExpandoObject))
        {
            return true;
        }
        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.WireType == WireType.Reference)
        {
            return ReferenceCodec.ReadReference<ExpandoObject, TInput>(ref reader, field)!;
        }

        // Written before this codec wrote its own header: the whole object went out marked as a dictionary, so
        // it arrives as one. Read it the way it was written, rather than failing on a message still in flight
        // from a silo that has not been upgraded yet. A header written by this codec carries either no type (it
        // was the type expected) or this one.
        if (field.FieldType is { } writtenAs && writtenAs != typeof(ExpandoObject))
        {
            return ToExpandoObject(codecProvider.GetCodec<Dictionary<string, object?>>().ReadValue(ref reader, field)!);
        }

        field.EnsureWireTypeTagDelimited();
        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
        string[] keys = [];
        object?[] values = [];
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
                    keys = codecProvider.GetCodec<string[]>().ReadValue(ref reader, header) ?? [];
                    break;
                case 1:
                    values = codecProvider.GetCodec<object?[]>().ReadValue(ref reader, header) ?? [];
                    break;
                default:
                    reader.ConsumeUnknownField(header);
                    break;
            }
        }

        var result = new ExpandoObject();
        var resultAsDictionary = (IDictionary<string, object?>)result;
        for (var index = 0; index < keys.Length; index++)
        {
            resultAsDictionary[keys[index]] = index < values.Length ? values[index] : null;
        }

        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
        return result;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The object is written under its own type rather than handed to the dictionary codec. Handed over, it went
    /// out marked as a dictionary, so every <see cref="ExpandoObject"/> nested inside it - a child in a collection,
    /// a nested object - came back on the receiving silo as a <see cref="Dictionary{TKey, TValue}"/>, and
    /// code that looks a child up by its properties could no longer find them. Its values are written as objects,
    /// so a nested one comes back through this codec too.
    /// </remarks>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] object? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        var expandoObject = (IDictionary<string, object?>)value;
        writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(ExpandoObject), WireType.TagDelimited);
        codecProvider.GetCodec<string[]>().WriteField(ref writer, 0, typeof(string[]), expandoObject.Keys.ToArray());
        codecProvider.GetCodec<object?[]>().WriteField(ref writer, 1, typeof(object?[]), expandoObject.Values.ToArray());
        writer.WriteEndObject();
    }

    static ExpandoObject ToExpandoObject(Dictionary<string, object?> dictionary)
    {
        var result = new ExpandoObject();
        var resultAsDictionary = (IDictionary<string, object?>)result;
        foreach (var (key, value) in dictionary)
        {
            resultAsDictionary[key] = value;
        }

        return result;
    }
}
