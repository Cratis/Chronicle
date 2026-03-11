// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Recommendations;

namespace Cratis.Chronicle.Cli.Commands.Recommendations;

/// <summary>
/// Lists recommendations for an event store.
/// </summary>
public class ListRecommendationsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, EventStoreSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var recommendations = await services.Recommendations.GetRecommendations(new GetRecommendationsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace()
        });

        var list = recommendations.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Id", "Name", "Type", "Description", "Occurred"],
            rec =>
            [
                rec.Id.ToString(),
                rec.Name,
                rec.Type,
                rec.Description,
                rec.Occurred?.ToString() ?? string.Empty
            ]);

        return ExitCodes.Success;
    }
}
