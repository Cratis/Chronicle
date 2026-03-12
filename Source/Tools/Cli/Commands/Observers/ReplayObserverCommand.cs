// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Replays an observer from the beginning.
/// </summary>
public class ReplayObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ObserverCommandSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        await services.Observers.Replay(new Replay
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        OutputFormatter.WriteMessage(format, $"Replay started for observer '{settings.ObserverId}'. Use 'cratis observers show {settings.ObserverId}' to check progress.");
        return ExitCodes.Success;
    }
}
