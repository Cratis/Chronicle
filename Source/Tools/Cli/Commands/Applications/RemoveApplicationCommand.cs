// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Removes an application (OAuth client) from the Chronicle system.
/// </summary>
public class RemoveApplicationCommand : ChronicleCommand<RemoveApplicationSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, RemoveApplicationSettings settings, string format)
    {
        await services.Applications.Remove(new RemoveApplication
        {
            Id = settings.AppId
        });

        OutputFormatter.WriteMessage(format, $"Application '{settings.AppId}' removed.");
        return ExitCodes.Success;
    }
}
