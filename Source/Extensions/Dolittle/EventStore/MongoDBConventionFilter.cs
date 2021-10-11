// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Represents an implementation of <see cref="ICanFilterMongoDBConventionPacksForType"/>.
    /// </summary>
    public class MongoDBConventionFilter : ICanFilterMongoDBConventionPacksForType
    {
        /// <inheritdoc/>
        public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
        {
            if (conventionPackName == ConventionPacks.CamelCase)
            {
                return !type.Namespace?.Equals(typeof(Event).Namespace) ?? true;
            }

            return true;
        }
    }
}
