// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore;

internal static partial class ChronicleClientWebApplicationBuilderLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Could not connect to the Chronicle kernel on startup. Reconnection continues in the background.")]
    internal static partial void CouldNotConnectOnStartup(this ILogger<IApplicationBuilder> logger, Exception exception);
}
