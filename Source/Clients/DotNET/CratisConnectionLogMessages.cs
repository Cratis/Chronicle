// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

internal static partial class CratisConnectionLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void Connecting(this ILogger<CratisConnection> logger);
}
