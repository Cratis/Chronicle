// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting;

/// <summary>
/// Represents an implementation of <see cref="IChronicleBuilder"/>.
/// </summary>
/// <param name="services"><see cref="IServiceCollection"/> to use.</param>
/// <param name="configuration"><see cref="IConfiguration"/> to use.</param>
public class ChronicleBuilder(IServiceCollection services, IConfiguration configuration) : IChronicleBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc/>
    public IConfiguration Configuration { get; } = configuration;
}
