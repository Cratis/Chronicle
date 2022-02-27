// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Orleans;

/// <summary>
/// Log messages for <see cref="ClusterConfigurationExtensions"/>.
/// </summary>
internal static partial class ClusterConfigurationExtensionsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Configuring localhost clustering - typically used in development.")]
    internal static partial void UsingLocalHostClustering(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "Configuring static non reliable clustering.")]
    internal static partial void UsingStaticClustering(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Configuring AdoNet reliable clustering.")]
    internal static partial void UsingAdoNetClustering(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "Configuring Azure Storage reliable clustering.")]
    internal static partial void UsingAzureStorageClustering(this ILogger logger);
}
