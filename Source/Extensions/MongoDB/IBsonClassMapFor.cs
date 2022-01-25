// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Defines a map for mapping a type to bson for MongoDB.
    /// </summary>
    /// <typeparam name="T">Type the class map is for.</typeparam>
    public interface IBsonClassMapFor<T>
    {
        /// <summary>
        /// Configure the given class map.
        /// </summary>
        /// <param name="classMap"><see cref="BsonClassMap{T}"/> to register.</param>
        void Configure(BsonClassMap<T> classMap);
    }
}
