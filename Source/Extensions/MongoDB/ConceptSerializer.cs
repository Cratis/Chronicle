// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Aksio.Cratis.Concepts;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a <see cref="IBsonSerializer{T}"/> for <see cref="ConceptAs{T}"/> types.
/// </summary>
/// <typeparam name="T">Type of concept.</typeparam>
public class ConceptSerializer<T> : IBsonSerializer<T>
{
    /// <inheritdoc/>
    public Type ValueType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConceptSerializer{T}"/> class.
    /// </summary>
    public ConceptSerializer()
    {
        ValueType = typeof(T);

        if (!ValueType.IsConcept())
            throw new TypeIsNotAConcept(ValueType);
    }

    /// <inheritdoc/>
    public T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;

        var actualType = args.NominalType;
        var bsonType = bsonReader.GetCurrentBsonType();

        var valueType = actualType.GetConceptValueType();

        object value;

        // It should be a Concept object
        if (bsonType == BsonType.Document)
        {
            bsonReader.ReadStartDocument();
            var keyName = bsonReader.ReadName(Utf8NameDecoder.Instance);
            if (keyName == "Value" || keyName == "value")
            {
                value = GetDeserializedValue(valueType, ref bsonReader);
                bsonReader.ReadEndDocument();
            }
            else
            {
                throw new FailedConceptSerialization("Expected a concept object, but no key named 'Value' or 'value' was found on the object");
            }
        }
        else
        {
            value = GetDeserializedValue(valueType, ref bsonReader);
        }

        return (dynamic)ConceptFactory.CreateConceptInstance(ValueType, value);
    }

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        var underlyingValue = value?.GetType()!.GetTypeInfo()!.GetProperty(nameof(ConceptAs<object>.Value))!.GetValue(value, null);
        var nominalType = args.NominalType;
        var underlyingValueType = nominalType.GetConceptValueType();

        var bsonWriter = context.Writer;

        if (underlyingValueType == typeof(Guid))
        {
            var guid = (Guid)(underlyingValue ?? default(Guid));
            bsonWriter.WriteBinaryData(new BsonBinaryData(guid, GuidRepresentation.Standard));
        }
        else if (underlyingValueType == typeof(double))
        {
            bsonWriter.WriteDouble((double)(underlyingValue ?? default(double)));
        }
        else if (underlyingValueType == typeof(float))
        {
            bsonWriter.WriteDouble((double)(underlyingValue ?? default(double)));
        }
        else if (underlyingValueType == typeof(int) || underlyingValueType == typeof(uint))
        {
            if (underlyingValue is uint)
            {
                underlyingValue = Convert.ChangeType(underlyingValue, typeof(int), CultureInfo.InvariantCulture)!;
            }

            bsonWriter.WriteInt32((int)(underlyingValue ?? default(int)));
        }
        else if (underlyingValueType == typeof(long) || underlyingValueType == typeof(ulong))
        {
            if (underlyingValue is ulong)
            {
                underlyingValue = Convert.ChangeType(underlyingValue, typeof(long), CultureInfo.InvariantCulture)!;
            }

            bsonWriter.WriteInt64((long)(underlyingValue ?? default(long)));
        }
        else if (underlyingValueType == typeof(bool))
        {
            bsonWriter.WriteBoolean((bool)(underlyingValue ?? default(bool)));
        }
        else if (underlyingValueType == typeof(string))
        {
            bsonWriter.WriteString((string)(underlyingValue ?? string.Empty));
        }
        else if (underlyingValueType == typeof(decimal))
        {
            bsonWriter.WriteDecimal128((decimal)(underlyingValue ?? default(decimal)));
        }
    }

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        Serialize(context, args, (object)value!);
    }

    /// <inheritdoc/>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args)!;

    object GetDeserializedValue(Type valueType, ref IBsonReader bsonReader)
    {
        var bsonType = bsonReader.CurrentBsonType;
        if (bsonType == BsonType.Null)
        {
            bsonReader.ReadNull();
            return null!;
        }

        if (valueType == typeof(Guid))
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.String)
            {
                return Guid.Parse(bsonReader.ReadString());
            }
            var binaryData = bsonReader.ReadBinaryData();
            return binaryData.ToGuid();
        }

        if (valueType == typeof(double))
        {
            return bsonReader.ReadDouble();
        }

        if (valueType == typeof(float))
        {
            return (float)bsonReader.ReadDouble();
        }

        if (valueType == typeof(int) || valueType == typeof(uint))
        {
            var value = bsonReader.ReadInt32();
            if (valueType == typeof(uint))
            {
                return Convert.ChangeType(value, typeof(uint), CultureInfo.InvariantCulture)!;
            }
            return value;
        }

        if (valueType == typeof(long) || valueType == typeof(ulong))
        {
            var value = bsonReader.ReadInt64();
            if (valueType == typeof(ulong))
            {
                return Convert.ChangeType(value, typeof(ulong), CultureInfo.InvariantCulture)!;
            }
            return value;
        }

        if (valueType == typeof(bool))
        {
            return bsonReader.ReadBoolean();
        }

        if (valueType == typeof(string))
        {
            return bsonReader.ReadString();
        }

        if (valueType == typeof(decimal))
        {
            return bsonReader.ReadDecimal128();
        }

        throw new FailedConceptSerialization($"Could not deserialize the concept value to '{valueType.FullName}'");
    }
}
