// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore;

internal static partial class ChronicleClientWebApplicationBuilderLogMessages
{
    [LoggerMessage(LogLevel.Warning, "UseCratisChronicle() was not called, so the Chronicle middleware and connection startup are not installed. Call app.UseCratisChronicle() after routing, authentication and authorization.")]
    internal static partial void UseCratisChronicleNotCalled(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "The Chronicle event store namespace is taken from the '{HeaderName}' HTTP header, which any caller can set. Unless a trusted proxy sets it, configure a namespace resolver that derives it from the authenticated user.")]
    internal static partial void NamespaceFromHttpHeader(this ILogger logger, string headerName);

    [LoggerMessage(LogLevel.Warning, "Could not connect to the Chronicle kernel on startup. Reconnection continues in the background.")]
    internal static partial void CouldNotConnectOnStartup(this ILogger<IApplicationBuilder> logger, Exception exception);
}
