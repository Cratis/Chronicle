// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a custom convention for setting the default discriminator on class maps.
/// </summary>
public class CustomDiscriminatorConvention : ConventionBase, IClassMapConvention
{
    readonly IDiscriminatorConvention _convention;
    readonly IEnumerable<Type> _typesWithDiscriminatorConvention;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomDiscriminatorConvention"/> class.
    /// </summary>
    /// <param name="convention"><see cref="IDiscriminatorConvention"/> to use.</param>
    /// <param name="typesWithDiscriminatorConvention">Collection of <see cref="Type"/> that already has a discriminator convention. </param>
    public CustomDiscriminatorConvention(IDiscriminatorConvention convention, IEnumerable<Type> typesWithDiscriminatorConvention)
    {
        _convention = convention;
        _typesWithDiscriminatorConvention = typesWithDiscriminatorConvention;
    }

    /// <inheritdoc/>
    public void Apply(BsonClassMap classMap)
    {
        var type = classMap.ClassType;

        if (!_typesWithDiscriminatorConvention.Contains(type) &&
            type.IsClass
            && type != typeof(string)
            && type != typeof(object)
            && !type.IsAbstract)
        {
            classMap.SetDiscriminator(_convention.GetDiscriminator(type, type).ToString());
        }
    }
}
