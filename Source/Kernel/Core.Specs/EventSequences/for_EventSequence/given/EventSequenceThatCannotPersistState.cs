// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An <see cref="EventSequence"/> whose grain state can never be written, standing in for the storage being
/// unavailable at the moment the warm-start snapshot is taken after a durable append.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="constraintValidatorSetFactory"><see cref="IConstraintValidationFactory"/> for creating a set of constraint validators.</param>
/// <param name="eventTypeMigrations"><see cref="IEventTypeMigrations"/> for migrating events between generations.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <param name="eventHashCalculator"><see cref="IEventHashCalculator"/> for calculating event content hashes.</param>
/// <param name="options"><see cref="IOptions{T}"/> for <see cref="ChronicleOptions"/>.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
/// <param name="concurrencyValidatorLogger"><see cref="ILogger{T}"/> for the concurrency validator.</param>
public class EventSequenceThatCannotPersistState(
    IStorage storage,
    IConstraintValidationFactory constraintValidatorSetFactory,
    IEventTypeMigrations eventTypeMigrations,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<EventSequence> meter,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<EventSequence> activitySource,
    IJsonComplianceManager jsonComplianceManagerProvider,
    IExpandoObjectConverter expandoObjectConverter,
    IEventSerializer eventSerializer,
    IEventHashCalculator eventHashCalculator,
    IOptions<ChronicleOptions> options,
    ILogger<EventSequence> logger,
    ILogger<Concurrency.ConcurrencyValidator> concurrencyValidatorLogger) : EventSequence(
        storage,
        constraintValidatorSetFactory,
        eventTypeMigrations,
        meter,
        activitySource,
        jsonComplianceManagerProvider,
        expandoObjectConverter,
        eventSerializer,
        eventHashCalculator,
        options,
        logger,
        concurrencyValidatorLogger)
{
    /// <inheritdoc/>
    protected override Task WriteStateAsync() => throw new SimulatedStateWriteError();
}
