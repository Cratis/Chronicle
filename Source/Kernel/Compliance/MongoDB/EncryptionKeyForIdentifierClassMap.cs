// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents a class map for <see cref="EncryptionKeyForIdentifier"/>.
    /// </summary>
    public class EncryptionKeyForIdentifierClassMap : IBsonClassMapFor<EncryptionKeyForIdentifier>
    {
        /// <inheritdoc/>
        public void Configure(BsonClassMap<EncryptionKeyForIdentifier> classMap)
        {
            classMap.AutoMap();
            classMap.MapIdProperty(_ => _.Identifier);
        }
    }
}
