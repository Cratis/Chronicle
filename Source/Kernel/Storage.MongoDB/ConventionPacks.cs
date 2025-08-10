// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Provides MongoDB convention packs.
/// </summary>
public class ConventionPacks : ICanProvideMongoDBConventionPacks
{
    /// <inheritdoc/>
    public IEnumerable<MongoDBConventionPackDefinition> Provide()
    {
        var predicate = new Func<Type, bool>(type => type.Namespace?.StartsWith("Cratis.Chronicle.Storage.MongoDB") == true);
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(predicate)
        };
        ConventionRegistry.Register("CamelCase", conventionPack, predicate);
        return [new("CamelCase", conventionPack)];
    }
}
