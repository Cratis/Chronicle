// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Cli.Commands.Users;

/// <summary>
/// Adds a new user to the Chronicle system.
/// </summary>
public class AddUserCommand : ChronicleCommand<AddUserSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, AddUserSettings settings, string format)
    {
        await services.Users.Add(new AddUser
        {
            UserId = Guid.NewGuid(),
            Username = settings.Username,
            Email = settings.Email,
            Password = settings.Password
        });

        OutputFormatter.WriteMessage(format, $"User '{settings.Username}' added.");
        return ExitCodes.Success;
    }
}
