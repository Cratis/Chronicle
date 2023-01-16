// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class ClientServiceProviderExtensionsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void ConnectingToKernel(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "Connected to Cratis Kernel")]
    internal static partial void ConnectedToKernel(this ILogger logger);

}
