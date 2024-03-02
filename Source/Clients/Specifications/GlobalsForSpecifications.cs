// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Types;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Holds the global instances used for testing.
/// </summary>
public static class GlobalsForSpecifications
{
    static GlobalsForSpecifications()
    {
        ClientArtifactsProvider = new DefaultClientArtifactsProvider(
            new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

        EventTypes = new Cratis.Events.EventTypes(ClientArtifactsProvider);
    }

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/>.
    /// </summary>
    public static IClientArtifactsProvider ClientArtifactsProvider { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/>.
    /// </summary>
    public static IEventTypes EventTypes { get; }
}
