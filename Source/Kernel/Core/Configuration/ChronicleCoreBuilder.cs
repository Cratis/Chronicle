// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IChronicleCoreBuilder"/>.
/// </summary>
/// <param name="siloBuilder"><see cref="ISiloBuilder"/> to use.</param>
/// <param name="services"><see cref="IServiceCollection"/> to use.</param>
/// <param name="configuration"><see cref="IConfiguration"/> to use.</param>
public class ChronicleCoreBuilder(ISiloBuilder siloBuilder, IServiceCollection services, IConfiguration configuration) : IChronicleCoreBuilder
{
    /// <inheritdoc/>
    public ISiloBuilder SiloBuilder { get; } = siloBuilder;

    /// <inheritdoc/>
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc/>
    public IConfiguration Configuration { get; } = configuration;
}
