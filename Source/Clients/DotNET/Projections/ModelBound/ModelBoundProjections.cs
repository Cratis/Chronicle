// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;
using Cratis.Types;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Discovers and builds projection definitions from model-bound attributes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelBoundProjections"/> class.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> for discovering types.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
public class ModelBoundProjections(
    ITypes types,
    INamingPolicy namingPolicy,
    IEventTypes eventTypes) : IModelBoundProjections
{
    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    public IEnumerable<ProjectionDefinition> Discover()
    {
        var builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes);

        // Find all types with model-bound projection attributes
        return types.All
            .Where(HasModelBoundProjectionAttributes)
            .Select(builder.Build);
    }

    static bool HasModelBoundProjectionAttributes(Type type)
    {
        // Check if type or its properties have KeyAttribute (indicating it's a projection root)
        if (type.GetProperties().Any(p => p.GetCustomAttribute<KeyAttribute>() is not null))
        {
            return true;
        }

        // Check if type has any projection mapping attributes using IProjectionAnnotation marker interface
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(property => property.GetCustomAttributes()
                                                  .Any(attr => attr is IProjectionAnnotation));
    }
}
