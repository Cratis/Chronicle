// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Extension methods for working with the <see cref="ClientConfiguration"/>.
/// </summary>
public static class ClientConfigurationExtensions
{
    /// <summary>
    /// Get the <see cref="SingleKernelOptions"/>.
    /// </summary>
    /// <param name="configuration"><see cref="ClientConfiguration"/> to get from.</param>
    /// <returns>An instance of <see cref="SingleKernelOptions"/>.</returns>
    /// <remarks>
    /// If the options is not defined or of the wrong type, the default instance will be returned.
    /// </remarks>
    public static SingleKernelOptions GetSingleKernelOptions(this ClientConfiguration configuration) => configuration.Options as SingleKernelOptions ?? new SingleKernelOptions();

    /// <summary>
    /// Get the <see cref="StaticClusterOptions"/>.
    /// </summary>
    /// <param name="configuration"><see cref="ClientConfiguration"/> to get from.</param>
    /// <returns>An instance of <see cref="StaticClusterOptions"/>.</returns>
    /// <remarks>
    /// If the options is not defined or of the wrong type, the default instance will be returned.
    /// </remarks>
    public static StaticClusterOptions GetStaticClusterOptions(this ClientConfiguration configuration) => configuration.Options as StaticClusterOptions ?? new StaticClusterOptions();

    /// <summary>
    /// Get the <see cref="AzureStorageClusterOptions"/>.
    /// </summary>
    /// <param name="configuration"><see cref="ClientConfiguration"/> to get from.</param>
    /// <returns>An instance of <see cref="AzureStorageClusterOptions"/>.</returns>
    /// <remarks>
    /// If the options is not defined or of the wrong type, the default instance will be returned.
    /// </remarks>
    public static AzureStorageClusterOptions GetAzureStorageClusterOptions(this ClientConfiguration configuration) => configuration.Options as AzureStorageClusterOptions ?? new AzureStorageClusterOptions();
}

