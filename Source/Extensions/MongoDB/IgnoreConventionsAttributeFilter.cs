// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Conventions;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="ICanFilterMongoDBConventionPacksForType"/> for filtering based on <see cref="IgnoreConventionsAttribute"/>.
    /// </summary>
    public class IgnoreConventionsAttributeFilter : ICanFilterMongoDBConventionPacksForType
    {
        /// <inheritdoc/>
        public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(IgnoreConventionsAttribute), false) as IgnoreConventionsAttribute[];
            if (attributes?.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.IgnoreAll) return false;
                    if (attribute.ConventionPacks.Contains(conventionPackName)) return false;
                }
            }
            return true;
        }
    }
}
