// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents a class map for <see cref="InternalSecretForPurpose"/>.
/// </summary>
public class InternalSecretForPurposeClassMap : IBsonClassMapFor<InternalSecretForPurpose>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<InternalSecretForPurpose> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
    }
}
