// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences;

public sealed partial class OccurrenceScenario<THandler>
{
    /// <summary>
    /// Asserts that a collected snapshot contains the effect receipt. Deliveries collect automatically; otherwise await StatusOf first.
    /// </summary>
    /// <param name="key">The effect identity.</param>
    /// <exception cref="OccurrenceScenarioAssertionFailed">The collected snapshot has no receipt for this key.</exception>
    public void ShouldHaveReceipt(EffectKey key)
    {
        if (!_snapshots.TryGetValue(key.Occurrence, out var snapshot) || snapshot?.Receipts.Any(receipt => receipt.Receipt.Id == key.Value) != true)
        {
            throw new OccurrenceScenarioAssertionFailed("Expected the collected occurrence to have the effect receipt.");
        }
    }

    /// <summary>
    /// Asserts that a collected occurrence snapshot is quarantined.
    /// </summary>
    /// <param name="id">The occurrence identity.</param>
    /// <exception cref="OccurrenceScenarioAssertionFailed">The collected occurrence is absent or not quarantined.</exception>
    public void ShouldBeQuarantined(OccurrenceId id)
    {
        if (!_snapshots.TryGetValue(id, out var snapshot) || snapshot?.State != OccurrenceState.Quarantined)
        {
            throw new OccurrenceScenarioAssertionFailed("Expected the collected occurrence to be quarantined.");
        }
    }

    /// <summary>
    /// Asserts that a delivery collected an appended event of the requested type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <exception cref="OccurrenceScenarioAssertionFailed">No event of this type was collected.</exception>
    public void ShouldHaveAppended<TEvent>() => ShouldHaveAppended<TEvent>(_ => true);

    /// <summary>
    /// Asserts that a delivery collected a matching appended event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="predicate">The event expectation.</param>
    /// <exception cref="OccurrenceScenarioAssertionFailed">No matching event was collected.</exception>
    public void ShouldHaveAppended<TEvent>(Func<TEvent, bool> predicate)
    {
        if (!_events.Select(@event => @event.Content).OfType<TEvent>().Any(predicate))
        {
            throw new OccurrenceScenarioAssertionFailed($"Expected a collected appended event of type '{typeof(TEvent).Name}'.");
        }
    }

    InMemoryClientArtifactsProvider Catalog()
    {
        var source = _defaults.ClientArtifactsProvider;
        return new()
        {
            EventTypes = source.EventTypes,
            Projections = source.Projections,
            ModelBoundProjections = source.ModelBoundProjections,
            Reactors = source.Reactors,
            ReadModelReactors = source.ReadModelReactors,
            Reducers = source.Reducers,
            ReactorMiddlewares = source.ReactorMiddlewares,
            ComplianceForTypesProviders = source.ComplianceForTypesProviders,
            ComplianceForPropertiesProviders = source.ComplianceForPropertiesProviders,
            SecurityForTypesProviders = source.SecurityForTypesProviders,
            SecurityForPropertiesProviders = source.SecurityForPropertiesProviders,
            AdditionalEventInformationProviders = source.AdditionalEventInformationProviders,
            ConstraintTypes = source.ConstraintTypes,
            UniqueConstraints = source.UniqueConstraints,
            UniqueEventTypeConstraints = source.UniqueEventTypeConstraints,
            RemoveConstraintEventTypes = source.RemoveConstraintEventTypes,
            ClosesStreamEventTypes = source.ClosesStreamEventTypes,
            EventTypeMigrators = source.EventTypeMigrators,
            EventSeeders = source.EventSeeders,
            EventSources = source.EventSources,
            Triggers = source.Triggers.Append(_triggerType).Distinct(),
            OccurrenceHandlers = [typeof(THandler)]
        };
    }
}
