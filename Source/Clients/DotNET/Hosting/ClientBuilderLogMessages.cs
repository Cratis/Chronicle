// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Hosting;

/// <summary>
/// Holds log messages for <see cref="ClientBuilder"/>.
/// </summary>
public static partial class ClientBuilderLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Configuring Cratis client")]
    internal static partial void Configuring(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "Configuring services")]
    internal static partial void ConfiguringServices(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Configuring compliance")]
    internal static partial void ConfiguringCompliance(this ILogger logger);
}
