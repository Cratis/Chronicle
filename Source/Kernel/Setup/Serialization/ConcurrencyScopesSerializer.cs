// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a custom Orleans serializer for <see cref="ConcurrencyScopes"/>.
/// </summary>
/// <param name="codecProvider">The <see cref="ICodecProvider"/>.</param>
public class ConcurrencyScopesSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is not ConcurrencyScopes original)
        {
            return input;
        }

        var copiedDictionary = new Dictionary<EventSourceId, ConcurrencyScope>();
        foreach (var kvp in original.Scopes)
        {
            copiedDictionary[kvp.Key] = kvp.Value;
        }

        return new ConcurrencyScopes(copiedDictionary);
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == typeof(ConcurrencyScopes);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == typeof(ConcurrencyScopes))
        {
            return true;
        }
        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        // Read the count first
        var countField = reader.ReadFieldHeader();
        var count = codecProvider.GetCodec<int>().ReadValue(ref reader, countField);

        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();

        for (var i = 0; i < count; i++)
        {
            var keyField = reader.ReadFieldHeader();
            var keyValue = codecProvider.GetCodec<string>().ReadValue(ref reader, keyField);
            var eventSourceId = new EventSourceId(keyValue);
            var valueField = reader.ReadFieldHeader();

            scopes[eventSourceId] = codecProvider.GetCodec<ConcurrencyScope>().ReadValue(ref reader, valueField);
        }

        return new ConcurrencyScopes(scopes);
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is not ConcurrencyScopes concurrencyScopes)
        {
            return;
        }

        codecProvider.GetCodec<int>().WriteField(ref writer, fieldIdDelta, typeof(int), concurrencyScopes.Scopes.Count);
        foreach (var kvp in concurrencyScopes.Scopes)
        {
            codecProvider.GetCodec<string>().WriteField(ref writer, fieldIdDelta, typeof(string), kvp.Key.Value);
            codecProvider.GetCodec<ConcurrencyScope>().WriteField(ref writer, fieldIdDelta, typeof(ConcurrencyScope), kvp.Value);
        }
    }
}
