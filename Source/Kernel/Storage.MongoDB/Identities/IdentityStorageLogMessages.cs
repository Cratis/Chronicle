// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Identities;

internal static partial class IdentityStorageLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Populating MongoDB Identity Store")]
    internal static partial void Populating(this ILogger<IdentityStorage> logger);

    [LoggerMessage(LogLevel.Trace, "Identity registered by user name with identifier {IdentityId}")]
    internal static partial void IdentityRegisteredByUserName(this ILogger<IdentityStorage> logger, IdentityId identityId);

    [LoggerMessage(LogLevel.Trace, "Trying to get a single identity")]
    internal static partial void TryingToGetSingleFor(this ILogger<IdentityStorage> logger);

    [LoggerMessage(LogLevel.Trace, "User found by subject with identifier {IdentityId}")]
    internal static partial void UserFoundBySubject(this ILogger<IdentityStorage> logger, IdentityId identityId);

    [LoggerMessage(LogLevel.Trace, "User found by name with identifier {IdentityId}")]
    internal static partial void UserFoundByName(this ILogger<IdentityStorage> logger, IdentityId identityId);

    [LoggerMessage(LogLevel.Trace, "User not found by name or subject")]
    internal static partial void UserNotFound(this ILogger<IdentityStorage> logger);

    [LoggerMessage(LogLevel.Trace, "Identity has no user name")]
    internal static partial void IdentityHasNoUserName(this ILogger<IdentityStorage> logger);
}
