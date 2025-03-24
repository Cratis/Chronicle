// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Identities;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class IdentityProviderLogging
{
    [LoggerMessage(LogLevel.Trace, "Identity not set")]
    internal static partial void IdentityNotSet(this ILogger<IdentityProvider> logger);

    [LoggerMessage(LogLevel.Trace, "Identity set to (Subject:'{subject}', Name:'{name}', Username:'{username}')")]
    internal static partial void IdentitySet(this ILogger<IdentityProvider> logger, string subject, string name, string username);
}
