// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class TokenProviderFactoryLogging
{
    [LoggerMessage(LogLevel.Information, "Requesting client tokens from the configured token endpoint {Endpoint} (source: Authentication:TokenEndpoint)")]
    internal static partial void UsingConfiguredTokenEndpoint(this ILogger logger, string endpoint);

    [LoggerMessage(LogLevel.Information, "Requesting client tokens from the token endpoint discovered from authority {Authority} (source: Authentication:Authority)")]
    internal static partial void UsingAuthorityDiscovery(this ILogger logger, string authority);

    [LoggerMessage(LogLevel.Information, "Requesting client tokens from the Chronicle server default token endpoint {Endpoint} (source: server default)")]
    internal static partial void UsingServerDefaultTokenEndpoint(this ILogger logger, string endpoint);
}
