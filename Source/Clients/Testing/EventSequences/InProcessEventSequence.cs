// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Reflection;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using KernelCompliance = KernelCore::Cratis.Chronicle.Compliance;
using KernelConstraints = KernelCore::Cratis.Chronicle.Events.Constraints;
using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;
using KernelMigrations = KernelCore::Cratis.Chronicle.EventSequences.Migrations;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Factory for creating and setting up a kernel <see cref="KernelEventSequences::EventSequence"/> grain for in-process testing.
/// </summary>
/// <remarks>
/// The kernel grain runs fully in-process without a real Orleans silo. Real implementations are used for all
/// dependencies except <see cref="KernelCore::Cratis.Chronicle.Storage.IStorage"/> (in-memory) and
/// <see cref="Cratis.Metrics.IMeter{T}"/> / <see cref="Microsoft.Extensions.Logging.ILogger{T}"/> (null implementations).
/// This means constraint validation, hash calculation, event serialization, migration, and compliance all run
/// through the actual kernel code paths.
/// </remarks>
internal static class InProcessEventSequence
{
    static readonly FieldInfo StorageField =
        typeof(global::Orleans.Grain<KernelEventSequences::EventSequenceState>)
            .GetField("_storage", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _storage field on Grain<EventSequenceState>.");

    static readonly FieldInfo EventSequenceKeyField =
        typeof(KernelEventSequences::EventSequence)
            .GetField("_eventSequenceKey", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _eventSequenceKey field on EventSequence.");

    static readonly FieldInfo EventSequenceIdField =
        typeof(KernelEventSequences::EventSequence)
            .GetField("_eventSequenceId", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _eventSequenceId field on EventSequence.");

    static readonly FieldInfo ConstraintsField =
        typeof(KernelEventSequences::EventSequence)
            .GetField("_constraints", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _constraints field on EventSequence.");

    /// <summary>
    /// Creates and initializes a kernel <see cref="KernelEventSequences::EventSequence"/> grain ready for in-process testing.
    /// </summary>
    /// <param name="storage">The in-memory kernel storage to use.</param>
    /// <param name="eventSequenceId">The <see cref="KernelSequenceConcepts::EventSequenceId"/> the grain represents.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The event store namespace name.</param>
    /// <returns>The initialized kernel <see cref="KernelEventSequences::EventSequence"/> grain.</returns>
    internal static async Task<KernelEventSequences::EventSequence> Create(
        InMemoryKernelStorage storage,
        KernelSequenceConcepts::EventSequenceId eventSequenceId,
        KernelConceptsNs::EventStoreName eventStoreName,
        KernelConceptsNs::EventStoreNamespaceName namespaceName)
    {
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);

        var eventTypeMigrations = new KernelMigrations::EventTypeMigrations(storage, expandoObjectConverter);
        var jsonComplianceManager = new KernelCompliance::JsonComplianceManager(new KnownInstancesOf<KernelCompliance::IJsonCompliancePropertyValueHandler>());
        var eventSerializer = new KernelEventSequences::EventSerializer(
            new InMemoryKernelEventTypes(),
            expandoObjectConverter,
            global::Cratis.Json.Globals.JsonSerializerOptions ?? new global::System.Text.Json.JsonSerializerOptions());

        var grain = new KernelEventSequences::EventSequence(
            storage,
            new KernelConstraints::ConstraintValidationFactory(storage),
            eventTypeMigrations,
            null!,
            jsonComplianceManager,
            expandoObjectConverter,
            eventSerializer,
            new KernelEventSequences::EventHashCalculator(),
            NullLogger<KernelEventSequences::EventSequence>.Instance);

        var grainStorage = new InMemoryGrainStorage<KernelEventSequences::EventSequenceState>();
        StorageField.SetValue(grain, grainStorage);

        var key = new KernelSequenceConcepts::EventSequenceKey(eventSequenceId, eventStoreName, namespaceName);
        EventSequenceKeyField.SetValue(grain, key);
        EventSequenceIdField.SetValue(grain, eventSequenceId);

        var constraintFactory = new KernelConstraints::ConstraintValidationFactory(storage);
        var constraints = await constraintFactory.Create(key);
        ConstraintsField.SetValue(grain, constraints);

        return grain;
    }
}
