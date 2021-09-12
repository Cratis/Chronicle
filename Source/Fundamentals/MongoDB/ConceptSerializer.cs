// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Concepts;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IBsonSerializer{T}"/> for <see cref="ConceptAs{T}"/> types.
    /// </summary>
    /// <typeparam name="T">Type of concept.</typeparam>
    public class ConceptSerializer<T> : IBsonSerializer<T>
    {
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
        public Type ValueType { get; }

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
                var guidAsBytes = guid.ToByteArray();
#pragma warning disable CS0618 // Obsolete - constructor is marked obsolete by the mongo driver - needs some investigation to make this correct.
                bsonWriter.WriteBinaryData(new BsonBinaryData(guidAsBytes, BsonBinarySubType.UuidLegacy, GuidRepresentation.CSharpLegacy));
#pragma warning restore
            }
            else if (underlyingValueType == typeof(double))
            {
                bsonWriter.WriteDouble((double)(underlyingValue ?? default(double)));
            }
            else if (underlyingValueType == typeof(float))
            {
                bsonWriter.WriteDouble((double)(underlyingValue ?? default(double)));
            }
            else if (underlyingValueType == typeof(int))
            {
                bsonWriter.WriteInt32((int)(underlyingValue ?? default(int)));
            }
            else if (underlyingValueType == typeof(long))
            {
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
                var binaryData = bsonReader.ReadBinaryData();
                return binaryData.ToGuid();
            }
            else if (valueType == typeof(double))
            {
                return bsonReader.ReadDouble();
            }
            else if (valueType == typeof(float))
            {
                return (float)bsonReader.ReadDouble();
            }
            else if (valueType == typeof(int))
            {
                return bsonReader.ReadInt32();
            }
            else if (valueType == typeof(long))
            {
                return bsonReader.ReadInt64();
            }
            else if (valueType == typeof(bool))
            {
                return bsonReader.ReadBoolean();
            }
            else if (valueType == typeof(string))
            {
                return bsonReader.ReadString();
            }
            else if (valueType == typeof(decimal))
            {
                return bsonReader.ReadDecimal128();
            }

            throw new FailedConceptSerialization($"Could not deserialize the concept value to '{valueType.FullName}'");
        }
    }
}
