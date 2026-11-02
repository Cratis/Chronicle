// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ExternalAuthorityTokenProviderLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Discovering token endpoint from authority {Authority}")]
    internal static partial void DiscoveringTokenEndpoint(this ILogger<ExternalAuthorityTokenProvider> logger, string authority);

    [LoggerMessage(LogLevel.Debug, "Discovered token endpoint {Endpoint} from authority {Authority}")]
    internal static partial void DiscoveredTokenEndpoint(this ILogger<ExternalAuthorityTokenProvider> logger, string authority, string endpoint);

    [LoggerMessage(LogLevel.Warning, "Failed to discover the token endpoint from authority {Authority}")]
    internal static partial void TokenEndpointDiscoveryFailed(this ILogger<ExternalAuthorityTokenProvider> logger, string authority, Exception ex);
}
