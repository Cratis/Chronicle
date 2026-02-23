// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

internal static partial class UsersReactorLogging
{
    [LoggerMessage(LogLevel.Trace, "Adding user with EventSourceId: {EventSourceId}, Username: {Username}")]
    internal static partial void AddingUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId, string username);

    [LoggerMessage(LogLevel.Debug, "User added with EventSourceId: {EventSourceId}")]
    internal static partial void UserAdded(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Trace, "Adding initial admin user with EventSourceId: {EventSourceId}, Username: {Username}")]
    internal static partial void AddingInitialAdminUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId, string username);

    [LoggerMessage(LogLevel.Debug, "Initial admin user added with EventSourceId: {EventSourceId}")]
    internal static partial void InitialAdminUserAdded(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Removing user with EventSourceId: {EventSourceId}")]
    internal static partial void RemovingUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "User removed with EventSourceId: {EventSourceId}")]
    internal static partial void UserRemoved(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Changing password for user with EventSourceId: {EventSourceId}")]
    internal static partial void ChangingPassword(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Password changed for user with EventSourceId: {EventSourceId}")]
    internal static partial void PasswordChanged(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Warning, "User not found when changing password for EventSourceId: {EventSourceId}")]
    internal static partial void UserNotFoundWhenChangingPassword(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Setting password change requirement for user with EventSourceId: {EventSourceId}")]
    internal static partial void SettingPasswordChangeRequirement(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Password change requirement set for user with EventSourceId: {EventSourceId}")]
    internal static partial void PasswordChangeRequirementSet(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Warning, "User not found when setting password change requirement for EventSourceId: {EventSourceId}")]
    internal static partial void UserNotFoundWhenSettingPasswordChangeRequirement(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);
}
