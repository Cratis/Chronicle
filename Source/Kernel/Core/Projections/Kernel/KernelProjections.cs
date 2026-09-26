// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IKernelProjections"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to discover declarations with.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to reach the managers with.</param>
/// <remarks>
/// Kernel projections are discovered by type rather than registered over the wire, which is what lets them exist
/// before any client connects. Registration is per event store and idempotent - every silo runs it on startup, and
/// both managers compare against what is already registered, so the ordinary case does no work.
/// </remarks>
[Singleton]
public class KernelProjections(
    ITypes types,
    IGrainFactory grainFactory) : IKernelProjections
{
    readonly IEnumerable<Type> _declarations = KernelProjectionDeclarations.Discover(types.All).ToArray();

    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore)
    {
        var readModels = new List<ReadModelDefinition>();
        var projections = new List<ProjectionDefinition>();

        foreach (var declaration in _declarations)
        {
            var (readModel, projectionsForDeclaration) = KernelProjectionDeclarations.Lower(declaration);
            readModels.Add(readModel);
            projections.AddRange(projectionsForDeclaration);
        }

        // Read models first: a projection definition names the read model it writes to, and the engine resolves
        // that name to a sink while it accepts the definition.
        await grainFactory.GetGrain<IReadModelsManager>(eventStore).Register(readModels);

        // Registered as the full set for the kernel owner, so a declaration deleted from the kernel has its
        // definition retired rather than left behind observing events nothing reads any more.
        await grainFactory.GetGrain<IProjectionsManager>(eventStore).Register(projections, ProjectionOwner.Kernel);
    }
}
