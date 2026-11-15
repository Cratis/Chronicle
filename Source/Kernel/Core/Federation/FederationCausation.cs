// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The causation recorded on every event forwarded into an inbox. It is the only durable cursor of a federation consumer:
/// the origin and source sequence number of the tail event of the inbox say where to resume.
/// </summary>
public static class FederationCausation
{
    /// <summary>
    /// The causation type.
    /// </summary>
    public const string Type = "Federation";

    /// <summary>
    /// The property holding the origin event store.
    /// </summary>
    public const string OriginEventStore = nameof(OriginEventStore);

    /// <summary>
    /// The property holding the sequence number in the origin.
    /// </summary>
    public const string SourceSequenceNumber = nameof(SourceSequenceNumber);

    /// <summary>
    /// The property holding the logical source of the binding.
    /// </summary>
    public const string LogicalSource = nameof(LogicalSource);

    /// <summary>
    /// Create the causation for a forwarded event.
    /// </summary>
    /// <param name="originEventStore">The origin event store.</param>
    /// <param name="sourceSequenceNumber">The sequence number in the origin.</param>
    /// <param name="logicalSource">The logical source of the binding.</param>
    /// <param name="occurred">When the event was forwarded.</param>
    /// <returns>The causation.</returns>
    public static Causation Create(string originEventStore, ulong sourceSequenceNumber, string logicalSource, DateTimeOffset occurred) =>
        new(
            occurred,
            Type,
            new Dictionary<string, string>
            {
                [OriginEventStore] = originEventStore,
                [SourceSequenceNumber] = sourceSequenceNumber.ToString(CultureInfo.InvariantCulture),
                [LogicalSource] = logicalSource
            });

    /// <summary>
    /// Read the resume point from the causation of an inbox event.
    /// </summary>
    /// <param name="causation">The causation chain of the event.</param>
    /// <returns>The resume point, or null when the event was not forwarded by federation.</returns>
    public static FederationResumePoint? Read(IEnumerable<Causation> causation)
    {
        foreach (var entry in causation)
        {
            if (entry.Type.Name == Type &&
                entry.Properties.TryGetValue(OriginEventStore, out var origin) &&
                entry.Properties.TryGetValue(SourceSequenceNumber, out var value) &&
                ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var sequenceNumber))
            {
                return new FederationResumePoint(origin, sequenceNumber);
            }
        }

        return null;
    }
}
