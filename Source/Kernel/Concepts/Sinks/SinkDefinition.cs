// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
/// <param name="Configuration">Unique <see cref="SinkConfigurationId"/> for the configuration.</param>
/// <param name="Type">Type of store.</param>
/// <param name="EventSequence">Event-sequence target metadata; required exactly when <paramref name="Type"/> is the event-sequence sink.</param>
public record SinkDefinition(SinkConfigurationId Configuration, SinkTypeId Type, EventSequenceSinkConfiguration? EventSequence = null)
{
    /// <summary>
    /// Gets the none representation of <see cref="SinkDefinition"/>.
    /// </summary>
    public static readonly SinkDefinition None = new(SinkConfigurationId.None, SinkTypeId.None);

    /// <summary>
    /// Refuses a sink definition whose type and event-target metadata disagree, so that metadata can never be
    /// ignored by a read-model sink and a missing target can never fall back to a read model or the event log.
    /// </summary>
    /// <exception cref="InconsistentEventSequenceSink">Thrown when the type and the event-target metadata disagree.</exception>
    public void EnsureReadModelSupported()
    {
        if ((EventSequence is null) == (Type == WellKnownSinkTypes.EventSequence))
        {
            throw new InconsistentEventSequenceSink();
        }

        EventSequence?.EnsureDestinationAllowed();
    }
}
