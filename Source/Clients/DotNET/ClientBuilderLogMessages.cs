// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Holds log messages for <see cref="ClientBuilder"/>.
/// </summary>
internal static partial class ClientBuilderLogMessages
{
    [LoggerMessage(LogLevel.Information, "Configuring Cratis client")]
    internal static partial void Configuring(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Configuring services")]
    internal static partial void ConfiguringServices(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Configuring compliance")]
    internal static partial void ConfiguringCompliance(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Using single kernel client @ '{Endpoint}'")]
    internal static partial void UsingSingleKernelClient(this ILogger logger, Uri endpoint);

    [LoggerMessage(LogLevel.Information, "Using static clustered kernel client")]
    internal static partial void UsingStaticClusterKernelClient(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Using Orleans Azure storage based clustered kernel client")]
    internal static partial void UsingOrleansAzureStorageKernelClient(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Using Inside Kernel client")]
    internal static partial void UsingInsideKernelClient(this ILogger logger);
}
