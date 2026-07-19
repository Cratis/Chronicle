// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Workbench;

internal static partial class WebServerLogMessages
{
    [LoggerMessage(LogLevel.Error, "The Chronicle Workbench web server failed")]
    internal static partial void WebServerFailed(this ILogger<WebServer> logger, Exception exception);
}
