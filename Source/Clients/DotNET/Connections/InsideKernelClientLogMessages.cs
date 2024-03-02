// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Connections;

internal static partial class InsideKernelClientLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Inside Kernel client will be connected to kernel on {Endpoint}")]
    internal static partial void InsideKernelConfigured(this ILogger<InsideKernelConnection> logger, Uri endpoint);
}
