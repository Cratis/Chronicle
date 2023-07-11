// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class InsideKernelClientLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Inside Kernel client will be connected to kernel on {Endpoint}")]
    internal static partial void InsideKernelConfigured(this ILogger<InsideKernelClient> logger, Uri endpoint);
}
