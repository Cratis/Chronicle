// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class TokenProviderFactoryLogging
{
    [LoggerMessage(LogLevel.Information, "Requesting client tokens from the {EndpointSource} token endpoint")]
    internal static partial void UsingTokenEndpoint(this ILogger logger, string endpointSource);
}
