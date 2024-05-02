// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Server;

/// <summary>
/// Log messages for Startup.
/// </summary>
internal static partial class StartupLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Kernel is configured with advertised IP: '{AdvertisedIP}'")]
    internal static partial void UsingAdvertisedIP(this ILogger<Startup> logger, string advertisedIP);

    [LoggerMessage(1, LogLevel.Information, "Kernel is configured without an advertised IP and will use the host name '{SiloHostName}'")]
    internal static partial void UsingSiloHostName(this ILogger<Startup> logger, string siloHostName);

    [LoggerMessage(2, LogLevel.Information, "Kernel is configured without an advertised IP and without a silo host name, using DNS resolved hostname: '{ResolvedHostName}'")]
    internal static partial void UsingResolvedHostName(this ILogger<Startup> logger, string resolvedHostName);

    [LoggerMessage(3, LogLevel.Information, "Using local host clustering")]
    internal static partial void UsingLocalHostClustering(this ILogger<Startup> logger);

    [LoggerMessage(4, LogLevel.Information, "Using static unreliable clustering")]
    internal static partial void UsingStaticHostClustering(this ILogger<Startup> logger);

    [LoggerMessage(5, LogLevel.Information, "Using ADO.net based clustering")]
    internal static partial void UsingAdoNetClustering(this ILogger<Startup> logger);

    [LoggerMessage(6, LogLevel.Information, "Using Azure storage clustering")]
    internal static partial void UsingAzureStorageClustering(this ILogger<Startup> logger);
}
