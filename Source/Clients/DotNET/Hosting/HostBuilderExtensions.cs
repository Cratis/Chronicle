// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for using Cratis.Chronicle in an application.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
    /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
    public static IHostBuilder AddCratisChronicle(
        this IHostBuilder hostBuilder)
    {
        ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();
        return hostBuilder;
    }
}
