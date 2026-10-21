// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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
/// Serializes ordered guards using a new type identity, never the legacy dictionary format.
/// </summary>
/// <param name="codecProvider">The codec provider.</param>
[RegisterSerializer]
[RegisterCopier]
public class ConcurrencyScopeConjunctionsSerializer(ICodecProvider codecProvider) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter, IFieldCodec<ConcurrencyScopeConjunctions>, IDeepCopier<ConcurrencyScopeConjunctions>
{
    /// <inheritdoc/>
    ConcurrencyScopeConjunctions IFieldCodec<ConcurrencyScopeConjunctions>.ReadValue<TInput>(ref Reader<TInput> reader, Field field) => (ConcurrencyScopeConjunctions)ReadValue(ref reader, field);

    /// <inheritdoc/>
    void IFieldCodec<ConcurrencyScopeConjunctions>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] ConcurrencyScopeConjunctions value) => WriteField(ref writer, fieldIdDelta, expectedType, value);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(input))]
    ConcurrencyScopeConjunctions? IDeepCopier<ConcurrencyScopeConjunctions>.DeepCopy(ConcurrencyScopeConjunctions? input, CopyContext context) => input is null ? null : (ConcurrencyScopeConjunctions)DeepCopy(input, context)!;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == typeof(ConcurrencyScopeConjunctions);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) => IsSupportedType(type) ? true : null;

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input is ConcurrencyScopeConjunctions scopes
        ? new ConcurrencyScopeConjunctions(scopes.Entries.Select(_ => (_.EventSourceId, _.Scope with { EventTypes = _.Scope.EventTypes?.ToArray() }, _.ScopeIndex)))
        : input;

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var count = codecProvider.GetCodec<int>().ReadValue(ref reader, field);
        var entries = new List<(EventSourceId, ConcurrencyScope, uint?)>();
        for (var i = 0; i < count; i++)
        {
            var keyField = reader.ReadFieldHeader();
            var key = codecProvider.GetCodec<string>().ReadValue(ref reader, keyField)!;
            var scopeField = reader.ReadFieldHeader();
            var scope = codecProvider.GetCodec<ConcurrencyScope>().ReadValue(ref reader, scopeField)!;
            var indexField = reader.ReadFieldHeader();
            var index = codecProvider.GetCodec<uint?>().ReadValue(ref reader, indexField);
            entries.Add((new(key), scope, index));
        }

        return new ConcurrencyScopeConjunctions(entries);
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] object? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (value is not ConcurrencyScopeConjunctions scopes) return;
        codecProvider.GetCodec<int>().WriteField(ref writer, fieldIdDelta, typeof(int), scopes.Entries.Count);
        foreach (var (id, scope, index) in scopes.Entries)
        {
            codecProvider.GetCodec<string>().WriteField(ref writer, fieldIdDelta, typeof(string), id.Value);
            codecProvider.GetCodec<ConcurrencyScope>().WriteField(ref writer, fieldIdDelta, typeof(ConcurrencyScope), scope);
            codecProvider.GetCodec<uint?>().WriteField(ref writer, fieldIdDelta, typeof(uint?), index);
        }
    }
}
