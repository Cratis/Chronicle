// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IBsonSerializer{T}"/> for handling serialization of <see cref="EncryptionKey"/>.
    /// </summary>
    public class EncryptionKeySerializer : IBsonSerializer<EncryptionKey>
    {
        /// <inheritdoc/>
        public Type ValueType => typeof(EncryptionKey);

        /// <inheritdoc/>
        public EncryptionKey Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => new(Convert.FromBase64String(context.Reader.ReadString()));

        /// <inheritdoc/>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EncryptionKey value) => context.Writer.WriteString(Convert.ToBase64String(value.Value));

        /// <inheritdoc/>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (EncryptionKey)value);

        /// <inheritdoc/>
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);
    }
}
