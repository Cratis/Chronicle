// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class ClientServiceCollectionExtensionsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void ConnectingToKernel(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "Using single kernel client @ '{Endpoint}'")]
    internal static partial void UsingSingleKernelClient(this ILogger logger, Uri endpoint);

    [LoggerMessage(2, LogLevel.Information, "Using static clustered kernel client")]
    internal static partial void UsingStaticClusterKernelClient(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "Using Orleans Azure storage based clustered kernel client")]
    internal static partial void UsingOrleansAzureStorageKernelClient(this ILogger logger);
}
