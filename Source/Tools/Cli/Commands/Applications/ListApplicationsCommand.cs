// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Lists all applications (OAuth clients) registered in the Chronicle system.
/// </summary>
public class ListApplicationsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, EventStoreSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);
        var applications = await services.Applications.GetAll();

        OutputFormatter.Write(
            format,
            applications,
            ["Id", "ClientId", "Active", "Created"],
            app =>
            [
                app.Id.ToString(),
                app.ClientId,
                app.IsActive.ToString(),
                app.CreatedAt.ToString()
            ]);

        return ExitCodes.Success;
    }
}
