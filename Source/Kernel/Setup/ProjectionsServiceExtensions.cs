// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1600
namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for projections initialization.
/// </summary>
public static class ProjectionsServiceExtensions
{
    /// <summary>
    /// Add projections.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddProjectionsService(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<ProjectionsService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IProjectionsServiceClient, ProjectionsServiceClient>());
        return siloBuilder;
    }
}
