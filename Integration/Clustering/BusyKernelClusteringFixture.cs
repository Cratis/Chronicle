// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// A clustering fixture whose projections manager can be made busy, so specs can register while the kernel is saturated.
/// </summary>
public class BusyKernelClusteringFixture : ClusteringFixture
{
    /// <summary>
    /// Gets the calls that make the projections manager busy.
    /// </summary>
    public BusyProjectionsManagerCalls Busy { get; } = new();

    /// <inheritdoc/>
    protected override void ConfigureFixtureServices(IServiceCollection services) =>
        services.AddSingleton<IIncomingGrainCallFilter>(Busy);
}
