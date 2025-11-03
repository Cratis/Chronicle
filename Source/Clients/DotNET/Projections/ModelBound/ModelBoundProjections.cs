// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Discovers and builds projection definitions from model-bound attributes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelBoundProjections"/> class.
/// </remarks>
/// <param name="clientArtifacts"><see cref="IClientArtifactsProvider"/> for discovering types.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class ModelBoundProjections(
    IClientArtifactsProvider clientArtifacts,
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions)
{
    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    public IEnumerable<ProjectionDefinition> Discover()
    {
        var builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes, jsonSerializerOptions);

        // Initialize client artifacts to ensure types are discovered
        clientArtifacts.Initialize();

        // Find all types with ReadModelAttribute from the discovered assemblies
        // Note: We scan all assemblies since model-bound projections are attributes on read models,
        // not on projection classes like IProjectionFor<T>
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return assemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<ReadModelAttribute>() is not null)
                    .Select(builder.Build)
                    .OfType<ProjectionDefinition>();
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                return [];
            }
        });
    }
}
