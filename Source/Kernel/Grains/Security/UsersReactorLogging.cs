// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Security;

internal static partial class UsersReactorLogging
{
    [LoggerMessage(LogLevel.Information, "Adding user with EventSourceId: {EventSourceId}, Username: {Username}")]
    internal static partial void AddingUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId, string username);

    [LoggerMessage(LogLevel.Information, "User added with EventSourceId: {EventSourceId}")]
    internal static partial void UserAdded(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Adding initial admin user with EventSourceId: {EventSourceId}, Username: {Username}")]
    internal static partial void AddingInitialAdminUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId, string username);

    [LoggerMessage(LogLevel.Information, "Initial admin user added with EventSourceId: {EventSourceId}")]
    internal static partial void InitialAdminUserAdded(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Removing user with EventSourceId: {EventSourceId}")]
    internal static partial void RemovingUser(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "User removed with EventSourceId: {EventSourceId}")]
    internal static partial void UserRemoved(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Changing password for user with EventSourceId: {EventSourceId}")]
    internal static partial void ChangingPassword(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Password changed for user with EventSourceId: {EventSourceId}")]
    internal static partial void PasswordChanged(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Warning, "User not found when changing password for EventSourceId: {EventSourceId}")]
    internal static partial void UserNotFoundWhenChangingPassword(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Setting password change requirement for user with EventSourceId: {EventSourceId}")]
    internal static partial void SettingPasswordChangeRequirement(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Password change requirement set for user with EventSourceId: {EventSourceId}")]
    internal static partial void PasswordChangeRequirementSet(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Warning, "User not found when setting password change requirement for EventSourceId: {EventSourceId}")]
    internal static partial void UserNotFoundWhenSettingPasswordChangeRequirement(this ILogger<UsersReactor> logger, EventSourceId eventSourceId);
}
