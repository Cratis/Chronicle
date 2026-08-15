// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents a class map for <see cref="EncryptionKeyErasureForIdentifier"/>.
/// </summary>
public class EncryptionKeyErasureForIdentifierClassMap : IBsonClassMapFor<EncryptionKeyErasureForIdentifier>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<EncryptionKeyErasureForIdentifier> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
    }
}
