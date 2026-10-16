// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Json;

namespace Cratis.Chronicle.ReadModels;

/// <summary>A concurrency conflict for a protected read.</summary>
/// <param name="ReadModelType">The read model that changed.</param>
/// <param name="Key">The key that changed.</param>
/// <remarks>
/// A conflict can be returned as a response body: System.Text.Json writes the read model's
/// <see cref="ReadModel">identifier</see>, the <see cref="Key"/> and the conflicting event, if known, as plain values,
/// with or without concept converters registered. The CLR <see cref="ReadModelType"/> is not serialized.
/// </remarks>
public record DecisionConflict(
    [property: JsonIgnore] Type ReadModelType,
    [property: JsonConverter(typeof(ConceptAsJsonConverterFactory))] ReadModelKey Key)
{
    /// <summary>
    /// Gets the <see cref="ReadModelIdentifier"/> of the read model that changed.
    /// </summary>
    [JsonConverter(typeof(ConceptAsJsonConverterFactory))]
    public ReadModelIdentifier ReadModel => ReadModelType.GetReadModelIdentifier();

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of the first event that changed the read model after it was read, if known.
    /// </summary>
    /// <remarks>
    /// Null when the conflict was not caused by a reported concurrency violation, or the kernel did not report the event.
    /// </remarks>
    [JsonConverter(typeof(ConceptAsJsonConverterFactory))]
    public EventTypeId? ConflictingEventTypeId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the first event that changed the read model after it was read, if known.
    /// </summary>
    /// <remarks>
    /// Null when the conflict was not caused by a reported concurrency violation, or the kernel did not report the event.
    /// </remarks>
    [JsonConverter(typeof(ConceptAsJsonConverterFactory))]
    public EventSequenceNumber? ConflictingEventSequenceNumber { get; init; }

    /// <summary>
    /// Creates a copy of this conflict carrying the conflicting event of a <see cref="ConcurrencyViolation"/>.
    /// </summary>
    /// <param name="violation">The <see cref="ConcurrencyViolation"/> that caused the conflict.</param>
    /// <returns>The conflict with the conflicting event, if the violation reported one.</returns>
    internal DecisionConflict CausedBy(ConcurrencyViolation violation) => this with
    {
        ConflictingEventTypeId = violation.ConflictingEventTypeId,
        ConflictingEventSequenceNumber = violation.ConflictingEventSequenceNumber
    };
}
