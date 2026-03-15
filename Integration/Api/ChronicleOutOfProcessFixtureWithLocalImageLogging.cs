// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api;

internal static partial class ChronicleOutOfProcessFixtureWithLocalImageLogMessages
{
    [LoggerMessage(LogLevel.Information, "Attempting to authenticate failed, will set admin user password and retry")]
    internal static partial void LogAttemptingToAuthenticateFailed(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);

    [LoggerMessage(LogLevel.Information, "Admin user already has password, skipping")]
    internal static partial void LogAdminUserAlreadyHasPassword(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);

    [LoggerMessage(LogLevel.Information, "Admin user exists, setting password")]
    internal static partial void LogAdminUserExistsSettingPassword(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);

    [LoggerMessage(LogLevel.Information, "Waiting for admin user to be created by reactor")]
    internal static partial void LogWaitingForAdminUserToBeCreated(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);

    [LoggerMessage(LogLevel.Information, "Password set successfully for admin user")]
    internal static partial void LogPasswordSetSuccessfully(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to set password for admin user - user may not exist")]
    internal static partial void LogFailedToSetPassword(this ILogger<ChronicleOutOfProcessFixtureWithLocalImage> logger);
}
