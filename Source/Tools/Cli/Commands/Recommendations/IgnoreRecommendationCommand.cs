// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Recommendations;

namespace Cratis.Chronicle.Cli.Commands.Recommendations;

/// <summary>
/// Ignores a recommendation.
/// </summary>
public class IgnoreRecommendationCommand : ChronicleCommand<RecommendationActionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, RecommendationActionSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        // The Ignore RPC reuses the Perform request message type per the protobuf contract.
        await services.Recommendations.Ignore(new Perform
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            RecommendationId = settings.RecommendationId
        });

        OutputFormatter.WriteMessage(format, $"Recommendation '{settings.RecommendationId}' ignored");
        return 0;
    }
}
