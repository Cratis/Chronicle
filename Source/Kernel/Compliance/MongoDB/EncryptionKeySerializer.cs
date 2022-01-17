// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IBsonSerializer{T}"/> for handling serialization of <see cref="EncryptionKey"/>.
    /// </summary>
    public class EncryptionKeySerializer : IBsonSerializer<byte[]>
    {
        /// <inheritdoc/>
        public Type ValueType => typeof(byte[]);

        /// <inheritdoc/>
        public byte[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Convert.FromBase64String(context.Reader.ReadString());

        /// <inheritdoc/>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, byte[] value) => context.Writer.WriteString(Convert.ToBase64String(value));

        /// <inheritdoc/>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (byte[])value);

        /// <inheritdoc/>
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);
    }
}
