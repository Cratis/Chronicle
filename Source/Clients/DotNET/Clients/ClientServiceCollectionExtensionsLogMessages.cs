// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class ClientServiceCollectionExtensionsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void ConnectingToKernel(this ILogger logger);
}
