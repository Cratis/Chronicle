// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a custom convention for setting the default discriminator on class maps.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomDiscriminatorConvention"/> class.
/// </remarks>
/// <param name="convention"><see cref="IDiscriminatorConvention"/> to use.</param>
/// <param name="typesWithDiscriminatorConvention">Collection of <see cref="Type"/> that already has a discriminator convention. </param>
public class CustomDiscriminatorConvention(IDiscriminatorConvention convention, IEnumerable<Type> typesWithDiscriminatorConvention) : ConventionBase, IClassMapConvention
{
    /// <inheritdoc/>
    public void Apply(BsonClassMap classMap)
    {
        var type = classMap.ClassType;

        if (!typesWithDiscriminatorConvention.Contains(type) &&
            type.IsClass
            && type != typeof(string)
            && type != typeof(object)
            && !type.IsAbstract)
        {
            classMap.SetDiscriminator(convention.GetDiscriminator(type, type).ToString());
        }
    }
}
