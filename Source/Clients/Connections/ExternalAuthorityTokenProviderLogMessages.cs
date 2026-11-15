// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ExternalAuthorityTokenProviderLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Discovering the token endpoint from the configured authority")]
    internal static partial void DiscoveringTokenEndpoint(this ILogger<ExternalAuthorityTokenProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Discovered the token endpoint from the configured authority")]
    internal static partial void DiscoveredTokenEndpoint(this ILogger<ExternalAuthorityTokenProvider> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to discover the token endpoint from the configured authority (reason: {Reason})")]
    internal static partial void TokenEndpointDiscoveryFailed(this ILogger<ExternalAuthorityTokenProvider> logger, string reason);
}
