// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Lists observers (reactors, reducers, projections) with optional type filtering.
/// </summary>
public class ListObserversCommand : ChronicleCommand<ListObserversSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ListObserversSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var observers = await services.Observers.GetObservers(new AllObserversRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace()
        });

        var filtered = FilterByType(observers, settings.Type);

        OutputFormatter.Write(
            format,
            filtered,
            ["Id", "Type", "State", "Next#", "LastHandled#", "Subscribed"],
            obs =>
            [
                obs.Id,
                obs.Type.ToString(),
                obs.RunningState.ToString(),
                obs.NextEventSequenceNumber.ToString(),
                obs.LastHandledEventSequenceNumber.ToString(),
                obs.IsSubscribed.ToString()
            ]);

        return ExitCodes.Success;
    }

    static IEnumerable<ObserverInformation> FilterByType(IEnumerable<ObserverInformation> observers, string type)
    {
        if (string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            return observers;
        }

        if (Enum.TryParse<ObserverType>(type, ignoreCase: true, out var parsed))
        {
            return observers.Where(o => o.Type == parsed);
        }

        return observers;
    }
}
