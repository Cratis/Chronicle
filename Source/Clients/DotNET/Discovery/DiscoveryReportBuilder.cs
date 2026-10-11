// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Serialization;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Builds a diagnostic snapshot from the client's current artifact collections.
/// </summary>
/// <param name="eventTypes">The event types.</param>
/// <param name="constraints">The constraints.</param>
/// <param name="reactors">The reactors.</param>
/// <param name="reducers">The reducers.</param>
/// <param name="projections">The projections.</param>
/// <param name="namingPolicy">The read model container naming policy.</param>
internal class DiscoveryReportBuilder(
    IEventTypes eventTypes,
    Constraints constraints,
    Reactors.Reactors reactors,
    IReducers reducers,
    Projections.Projections projections,
    INamingPolicy namingPolicy)
{
    /// <summary>
    /// Builds the report from successfully discovered or explicitly added artifacts.
    /// </summary>
    /// <returns>The current discovery report.</returns>
    internal DiscoveryReport Build()
    {
        var projectionHandlers = projections.GetAllHandlers().ToArray();
        var reducerHandlers = reducers.GetAllHandlers().ToArray();
        var readModels = projectionHandlers.Cast<IHaveReadModel>().Concat(reducerHandlers)
            .Select(_ => _.ReadModelType).Distinct();
        var artifacts = eventTypes.All.Select(_ => _.Id).Distinct()
            .Select(id => new DiscoveredArtifact(ArtifactKind.EventType, id.Value, eventTypes.GetClrTypeFor(id)))
            .Concat(constraints.Names.Select(_ => new DiscoveredArtifact(ArtifactKind.Constraint, _.Value, null)))
            .Concat(reactors.Handlers.Select(_ => new DiscoveredArtifact(ArtifactKind.Reactor, _.Id.Value, _.ReactorType == typeof(object) ? null : _.ReactorType)))
            .Concat(reducerHandlers.Select(_ => new DiscoveredArtifact(ArtifactKind.Reducer, _.Id.Value, _.ReducerType)))
            .Concat(projectionHandlers.Select(_ => new DiscoveredArtifact(ArtifactKind.Projection, _.Id.Value, ProjectionTypeFor(_))))
            .Concat(readModels.Select(type => new DiscoveredArtifact(
                ArtifactKind.ReadModel,
                ((IKnowReadModelContainerNames)projections).GetContainerNameOverride(type) ?? namingPolicy.GetReadModelName(type),
                type)))
            .OrderBy(_ => _.Kind).ThenBy(_ => _.Id, StringComparer.Ordinal).ToImmutableList();

        return new(true, artifacts);
    }

    Type? ProjectionTypeFor(IProjectionHandler handler) => projections.ArtifactRegistrations
        .Where(_ => _.IsRegistered)
        .Select(_ => _.ArtifactType)
        .FirstOrDefault(type => type.FullName == handler.Id.Value || type.GetCustomAttribute<ProjectionAttribute>()?.Id.Value == handler.Id.Value)
        ?? projections.ArtifactRegistrations.Where(_ => _.IsRegistered).Select(_ => _.ArtifactType)
            .FirstOrDefault(type => type == handler.ReadModelType);
}
