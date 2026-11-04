// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Reads handling progress without changing a step's status or original failure.
/// </summary>
internal static class HandleEventsStepResults
{
    /// <summary>
    /// Reads usable handling progress from a step result.
    /// </summary>
    /// <param name="result">The step result.</param>
    /// <param name="jsonSerializerOptions">The serializer options used to read the result.</param>
    /// <returns>The handling progress, or null when the payload cannot describe progress.</returns>
    /// <remarks>
    /// The result is untrusted payload: a thrown step can carry an empty string instead of handling progress.
    /// </remarks>
    internal static HandleEventsForPartitionResult? ReadProgress(JobStepResult result, JsonSerializerOptions jsonSerializerOptions)
    {
        if (result.TryGetError(out var error) && !CanCarryResult(error.PartialResult))
        {
            return null;
        }

        try
        {
            result.TryGetFullResult<HandleEventsForPartitionResult>(out var handled, out _, jsonSerializerOptions);
            return handled is { LastHandledEventSequenceNumber: not null, LastScannedEventSequenceNumber: not null } ? handled : null;
        }
        catch (JsonException)
        {
            // An unreadable payload records no handling progress. The step's own status and error are left as they are.
            return null;
        }
    }

    static bool CanCarryResult(object? partialResult) => partialResult switch
    {
        null or string => false,
        JsonElement element => element.ValueKind == JsonValueKind.Object,
        _ => true,
    };
}
