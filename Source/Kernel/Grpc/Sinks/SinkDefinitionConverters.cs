// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Sinks;

/// <summary>
/// Converter methods for <see cref="SinkDefinition"/>.
/// </summary>
internal static class SinkDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="SinkDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="SinkDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Sinks.SinkDefinition ToContract(this SinkDefinition definition)
    {
        return new()
        {
            ConfigurationId = definition.Configuration,
            TypeId = definition.EventSequence is null ? definition.Type : WellKnownSinkTypes.EventSequence,
            EventSequence = definition.EventSequence is null ? null : new()
            {
                EventType = new()
                {
                    Id = definition.EventSequence.EventType.Id,
                    Generation = definition.EventSequence.EventType.Generation,
                    Tombstone = definition.EventSequence.EventType.Tombstone
                },
                EventSequence = definition.EventSequence.Destination,
                IsPublic = definition.EventSequence.IsPublic
            }
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="SinkDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Sinks.SinkDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static SinkDefinition ToChronicle(this Contracts.Sinks.SinkDefinition contract)
    {
        var eventSequence = contract.EventSequence;
        var configuration = eventSequence is null ? null : new EventSequenceSinkConfiguration(
            new(eventSequence.EventType.Id, eventSequence.EventType.Generation, eventSequence.EventType.Tombstone),
            eventSequence.EventSequence is null ? null : new(eventSequence.EventSequence),
            eventSequence.IsPublic);

        return new(new SinkConfigurationId(contract.ConfigurationId), eventSequence is null ? contract.TypeId : WellKnownSinkTypes.EventSequence, configuration);
    }
}
