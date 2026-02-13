// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Wrapper serializer that adapts JobStateSerializer to derived types.
/// </summary>
/// <typeparam name="TDerived">The derived job state type.</typeparam>
/// <param name="baseSerializer">The base JobStateSerializer.</param>
sealed class JobStateSerializerWrapper<TDerived>(JobStateSerializer baseSerializer) : SerializerBase<TDerived>
    where TDerived : JobState
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDerived value)
        => baseSerializer.Serialize(context, args, value);

    /// <inheritdoc/>
    public override TDerived Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => (TDerived)baseSerializer.Deserialize(context, args);
}
