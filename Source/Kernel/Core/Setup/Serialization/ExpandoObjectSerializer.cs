// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
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
        var dictionaryCodec = codecProvider.GetCodec<Dictionary<string, object?>>();
        var dictionary = dictionaryCodec.ReadValue(ref reader, field);

        var result = new ExpandoObject();
        var resultDict = (IDictionary<string, object?>)result;
        foreach (var kvp in dictionary)
        {
            resultDict[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is not ExpandoObject expandoObject)
        {
            return;
        }

        var dictionary = new Dictionary<string, object?>(expandoObject);
        var dictionaryCodec = codecProvider.GetCodec<Dictionary<string, object?>>();
        dictionaryCodec.WriteField(ref writer, fieldIdDelta, expectedType, dictionary);
    }
}
