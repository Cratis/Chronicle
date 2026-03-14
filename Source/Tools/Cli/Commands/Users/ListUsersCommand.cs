// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.Users;

/// <summary>
/// Lists all users registered in the Chronicle system.
/// </summary>
public class ListUsersCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var users = await services.Users.GetAll();

        OutputFormatter.Write(
            format,
            users,
            ["Id", "Username", "Email", "Active", "HasLoggedIn", "Created"],
            user =>
            [
                user.Id.ToString(),
                user.Username,
                user.Email ?? string.Empty,
                user.IsActive.ToString(),
                user.HasLoggedIn.ToString(),
                user.CreatedAt.ToString()
            ]);

        return ExitCodes.Success;
    }
}
