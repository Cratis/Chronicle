// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a set of extensions for the Orleans serializer.
/// </summary>
public static class ConceptsSerializerExtensions
{
    /// <summary>
    /// Adds the concept serializer to the services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to extend.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddConceptSerializer(this IServiceCollection services)
    {
        services.AddSerializer(builder =>
        {
            builder.Services.AddSingleton<ConceptSerializer>();
            builder.Services.AddSingleton<IGeneralizedCodec, ConceptSerializer>();
            builder.Services.AddSingleton<IGeneralizedCopier, ConceptSerializer>();
            builder.Services.AddSingleton<ITypeFilter, ConceptSerializer>();
        });
        return services;
    }
}
