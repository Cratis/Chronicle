// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IKernelProjections"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to discover declarations with.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to reach the managers with.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>, for the storage the kernel runs on.</param>
/// <remarks>
/// Kernel projections are discovered by type rather than registered over the wire, which is what lets them exist
/// before any client connects. Registration is per event store and idempotent - every silo runs it on startup, and
/// both managers compare against what is already registered, so the ordinary case does no work.
/// </remarks>
[Singleton]
public class KernelProjections(
    ITypes types,
    IGrainFactory grainFactory,
    IOptions<ChronicleOptions> options) : IKernelProjections
{
    readonly IEnumerable<Type> _declarations = KernelProjectionDeclarations.Discover(types.All).ToArray();

    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore)
    {
        var readModels = new List<ReadModelDefinition>();
        var projections = new List<ProjectionDefinition>();

        foreach (var declaration in _declarations)
        {
            var (readModel, projectionsForDeclaration) = KernelProjectionDeclarations.Lower(declaration, KernelProjectionSinks.ForStorage(options.Value.Storage.Type));
            readModels.Add(readModel);
            projections.AddRange(projectionsForDeclaration);
        }

        // Read models first: a projection definition names the read model it writes to, and the engine resolves
        // that name to a sink while it accepts the definition.
        await grainFactory.GetGrain<IReadModelsManager>(eventStore).Register(readModels);

        // Registered as the full set for the kernel owner, so a declaration deleted from the kernel has its
        // definition retired rather than left behind observing events nothing reads any more.
        await RegisterProjections(eventStore, projections);
    }

    /// <summary>
    /// Registers kernel definitions through the shared path when the receiving silo supports it.
    /// </summary>
    /// <param name="eventStore">The event store to register with.</param>
    /// <param name="projections">The lowered kernel definitions.</param>
    /// <returns>A task representing the registration.</returns>
    internal async Task RegisterProjections(EventStoreName eventStore, IEnumerable<ProjectionDefinition> projections)
    {
        var manager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
        var signature = TryComputeSignature(projections);
        if (signature is not null)
        {
            try
            {
                await manager.RegisterShared(signature, projections, ProjectionOwner.Kernel);
                return;
            }
            catch (Exception exception) when (IncompatibleSiloRelease.IsLikely(exception))
            {
                // A silo running the previous interface can still accept the ordinary registration.
            }
        }

        await manager.Register(projections, ProjectionOwner.Kernel);
    }

    static string? TryComputeSignature(IEnumerable<ProjectionDefinition> projections)
    {
        try
        {
            return ProjectionRegistrationSignature.For(projections, ProjectionOwner.Kernel, null, null);
        }
        catch (Exception)
        {
            // Sharing is only an optimization; a definition that cannot be hashed still gets registered.
            return null;
        }
    }
}
