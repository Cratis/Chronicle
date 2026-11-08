// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Serializers;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Registers the BSON serializers required by mutation persistence documents before their class maps are frozen.
/// </summary>
static class EventSequenceMutationBsonSerialization
{
    static readonly object _gate = new();
    static bool _registered;

    /// <summary>
    /// Registers mutation serializers exactly once.
    /// </summary>
    internal static void Register()
    {
        lock (_gate)
        {
            if (_registered)
            {
                return;
            }

            Register(new EventSequenceIdentityKeySerializer());
            Register(new EventSequenceMutationIdentitySerializer());
            Register(new EventSequenceMutationDefinitionDigestV1Serializer());
            Register(new EventSequenceMutationReceiptDigestV1Serializer());
            _registered = true;
        }
    }

    static void Register<T>(IBsonSerializer<T> required)
    {
        if (BsonSerializer.TryRegisterSerializer(required))
        {
            return;
        }

        var actual = BsonSerializer.LookupSerializer<T>();
        if (actual.GetType() != required.GetType())
        {
            throw new IncompatibleEventSequenceMutationBsonSerializer(typeof(T), required.GetType(), actual.GetType());
        }
    }
}
