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
    static readonly Type _codecType = typeof(ConceptSerializer);

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == _codecType || type.IsConcept();

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == _codecType || type.IsConcept())
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
            return ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
        }

        field.EnsureWireTypeTagDelimited();
        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);

        Type? conceptType = null;
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
                    conceptType = TypeSerializerCodec.ReadValue(ref reader, header);
                    break;
                case 1:
                    value = ObjectCodec.ReadValue(ref reader, header);
                    break;
                default:
                    reader.ConsumeUnknownField(header);
                    break;
            }
        }

        if (conceptType is null || value is null)
        {
            return null!;
        }

        var concept = ConceptFactory.CreateConceptInstance(conceptType, value);
        ReferenceCodec.RecordObject(reader.Session, concept, placeholderReferenceId);
        return concept;
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        // Handles null and back-references (and registers this value in the reference table so the
        // read side, which calls CreateRecordPlaceholder, stays in sync).
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        // Concepts are serialized self-describingly: the concept type is written as field 0 and
        // the underlying primitive value as field 1. The concept type MUST be on the wire because
        // a single generalized codec serves every concept type, and the read path receives no
        // expected-type context to recover it from. Without this, cross-silo grain calls (the only
        // path that actually serializes — local calls deep-copy) desync the stream.
        writer.WriteFieldHeader(fieldIdDelta, expectedType, _codecType, WireType.TagDelimited);

        TypeSerializerCodec.WriteField(ref writer, 0, value.GetType());
        ObjectCodec.WriteField(ref writer, 1, value.GetConceptValue());

        writer.WriteEndObject();
    }
}
