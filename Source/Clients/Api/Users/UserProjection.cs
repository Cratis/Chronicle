// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the projection for <see cref="User"/>.
/// </summary>
public class UserProjection : IProjectionFor<User>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<InitialAdminUserAdded>(b => b
            .Set(m => m.Username).To(e => e.Username)
            .Set(m => m.HasLoggedIn).ToValue(false)
            .Set(m => m.PasswordChangeRequired).ToValue(true)
            .Set(m => m.PasswordHash).ToValue(string.Empty))
        .From<PasswordChanged>(b => b
            .Set(m => m.PasswordHash).To(e => e.PasswordHash)
            .Set(m => m.PasswordChangeRequired).ToValue(false))
        .From<PasswordChangeRequired>(b => b
            .Set(m => m.PasswordChangeRequired).ToValue(true))
        .From<UserLoggedIn>(b => b
            .Set(m => m.HasLoggedIn).ToValue(true));
}
