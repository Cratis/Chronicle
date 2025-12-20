// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

<<<<<<<< HEAD:Source/Kernel/Storage.MongoDB/Serialization/SiloAddressSerializer.cs
namespace Cratis.Chronicle.Storage.MongoDB.Serialization;
========
namespace Cratis.Chronicle.Storage.MongoDB.Serializers;
>>>>>>>> main:Source/Kernel/Storage.MongoDB/Serializers/SiloAddressSerializer.cs

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="SiloAddress"/>.
/// </summary>
public class SiloAddressSerializer : SerializerBase<SiloAddress>
{
    /// <inheritdoc/>
    public override SiloAddress Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var address = context.Reader.ReadString();
        return SiloAddress.FromParsableString(address);
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SiloAddress value)
    {
        context.Writer.WriteString(value.ToParsableString());
    }
}
