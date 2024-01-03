// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.MongoDB.Identities;

internal static partial class MongoDBIdentityStorageLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Populating MongoDB Identity Store")]
    internal static partial void Populating(this ILogger<MongoDBIdentityStorage> logger);

    [LoggerMessage(1, LogLevel.Trace, "Identity registered by user name {UserName} with identifier {IdentityId}")]
    internal static partial void IdentityRegisteredByUserName(this ILogger<MongoDBIdentityStorage> logger, string userName, IdentityId identityId);

    [LoggerMessage(2, LogLevel.Trace, "Trying to get single for {UserName} and {Subject}")]
    internal static partial void TryingToGetSingleFor(this ILogger<MongoDBIdentityStorage> logger, string userName, string subject);

    [LoggerMessage(3, LogLevel.Trace, "User found by subject {Subject} with identifier {IdentityId}")]
    internal static partial void UserFoundBySubject(this ILogger<MongoDBIdentityStorage> logger, string subject, IdentityId identityId);

    [LoggerMessage(4, LogLevel.Trace, "User found by name {UserName} with identifier {IdentityId}")]
    internal static partial void UserFoundByName(this ILogger<MongoDBIdentityStorage> logger, string userName, IdentityId identityId);

    [LoggerMessage(5, LogLevel.Trace, "User not found for {UserName} and {Subject}")]
    internal static partial void UserNotFound(this ILogger<MongoDBIdentityStorage> logger, string userName, string subject);
}
