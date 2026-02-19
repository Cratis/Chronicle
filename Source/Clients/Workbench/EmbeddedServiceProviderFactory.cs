// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Workbench;

/// <summary>
/// Represents a factory for creating an embedded <see cref="IServiceProvider"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public class EmbeddedServiceProviderFactory(IServiceProvider serviceProvider) : IServiceProviderFactory<IServiceCollection>
{
    /// <inheritdoc/>
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) => serviceProvider;
}
