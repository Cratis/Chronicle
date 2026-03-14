// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Adds a new application (OAuth client) to the Chronicle system.
/// </summary>
public class AddApplicationCommand : ChronicleCommand<AddApplicationSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, AddApplicationSettings settings, string format)
    {
        await services.Applications.Add(new AddApplication
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret
        });

        OutputFormatter.WriteMessage(format, $"Application '{settings.ClientId}' added.");
        return ExitCodes.Success;
    }
}
