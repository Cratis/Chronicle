// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert to and from <see cref="Uri"/>.
/// </summary>
public class UriSerializer : SerializerBase<Uri>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Uri value)
    {
        context.Writer.WriteString(value.ToString());
    }

    /// <inheritdoc/>
    public override Uri Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new Uri(context.Reader.ReadString());
    }
}
