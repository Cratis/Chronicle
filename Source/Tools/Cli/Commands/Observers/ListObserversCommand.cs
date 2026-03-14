// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Lists observers (reactors, reducers, projections) with optional type filtering.
/// </summary>
public class ListObserversCommand : ChronicleCommand<ListObserversSettings>
{
    /// <summary>
    /// Filters observers by type name, returning all observers when the type is "all".
    /// </summary>
    /// <param name="observers">The observers to filter.</param>
    /// <param name="type">The type name to filter by (e.g. "reactor", "projection", "all").</param>
    /// <returns>Filtered observers matching the specified type.</returns>
    internal static IEnumerable<ObserverInformation> FilterByType(IEnumerable<ObserverInformation> observers, string type)
    {
        if (string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            return observers;
        }

        // Validation already passed in IsValidType, so TryParse is guaranteed to succeed.
        Enum.TryParse<ObserverType>(type, ignoreCase: true, out var parsed);
        return observers.Where(o => o.Type == parsed);
    }

    /// <summary>
    /// Validates whether the given type string is a recognized observer type or "all".
    /// </summary>
    /// <param name="type">The type string to validate.</param>
    /// <param name="errorMessage">When invalid, contains the error description.</param>
    /// <returns><see langword="true"/> if the type is valid; otherwise <see langword="false"/>.</returns>
    internal static bool IsValidType(string type, out string errorMessage)
    {
        if (string.Equals(type, "all", StringComparison.OrdinalIgnoreCase) ||
            Enum.TryParse<ObserverType>(type, ignoreCase: true, out _))
        {
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = $"Invalid observer type '{type}'";
        return false;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ListObserversSettings settings, string format)
    {
        if (!IsValidType(settings.Type, out var errorMessage))
        {
            OutputFormatter.WriteError(format, errorMessage, "Valid types: reactor, reducer, projection, all");
            return ExitCodes.ValidationError;
        }

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
}
