// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Awaits the persisted credentials, not merely acceptance of their event. Reads shared storage so
/// completion on another silo is visible, including SQL providers without cluster-wide notifications.
/// </summary>
internal static class UserProjection
{
    /// <summary>
    /// Waits until the precise appended credential is available to sign-in requests.
    /// </summary>
    /// <param name="users">Shared user storage.</param>
    /// <param name="userId">The user to await.</param>
    /// <param name="passwordHash">The appended credential hash.</param>
    /// <param name="timeout">The maximum completion time.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="UserProjectionDidNotComplete">Thrown when credentials do not become available before the deadline.</exception>
    internal static Task WaitForPassword(IUserStorage users, UserId userId, UserPassword passwordHash, TimeSpan? timeout = null) =>
        WaitFor(users, userId, user => user.HasLoggedIn && user.PasswordHash == passwordHash, timeout);

    /// <summary>
    /// Waits until administrator creation has actually reached shared user storage.
    /// </summary>
    /// <param name="users">Shared user storage.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="username">The configured administrator username.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task WaitForAdministrator(IUserStorage users, UserId userId, Username username) =>
        WaitFor(users, userId, user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Waits for credentials initialized by a competing bootstrap request.
    /// </summary>
    /// <param name="users">Shared user storage.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task WaitForInitializedPassword(IUserStorage users, UserId userId) =>
        WaitFor(users, userId, user => user.HasLoggedIn && !string.IsNullOrEmpty(user.PasswordHash?.Value));

    /// <summary>
    /// Waits for the first-login password change requirement to be persisted.
    /// </summary>
    /// <param name="users">Shared user storage.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task WaitForPasswordChangeRequirement(IUserStorage users, UserId userId) =>
        WaitFor(users, userId, user => user.RequiresPasswordChange);

    static async Task WaitFor(IUserStorage users, UserId userId, Func<Storage.Security.User, bool> completed, TimeSpan? timeout = null)
    {
        using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        try
        {
            while (true)
            {
                var user = await users.GetById(userId).WaitAsync(deadline.Token);
                if (user is not null && completed(user))
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), deadline.Token);
            }
        }
        catch (OperationCanceledException)
        {
            throw new UserProjectionDidNotComplete();
        }
    }
}
