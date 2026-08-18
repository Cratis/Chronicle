// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
using System.Reflection;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Compliance;
using Cratis.Traces;
using Microsoft.Extensions.Options;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelConfiguration = KernelCore::Cratis.Chronicle.Configuration;
using KernelConstraints = KernelCore::Cratis.Chronicle.Events.Constraints;
using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;
using KernelMigrations = KernelCore::Cratis.Chronicle.EventSequences.Migrations;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Factory for creating and setting up a kernel <see cref="KernelEventSequences::EventSequence"/> grain for in-process testing.
/// </summary>
/// <remarks>
/// The kernel grain runs fully in-process without a real Orleans silo. Real implementations are used for all
/// dependencies except <c>IStorage</c> (in-memory) and
/// <see cref="Metrics.IMeter{T}"/> / <see cref="Microsoft.Extensions.Logging.ILogger{T}"/> (null implementations).
/// This means constraint validation, hash calculation, event serialization, migration, and compliance all run
/// through the actual kernel code paths. Compliance encrypts <c>[PII]</c> with the kernel's real
/// <c>PIICompliancePropertyValueHandler</c> over an in-memory encryption key store — see
/// <see cref="InProcessCompliance"/>, whose instance must be the same one the reading side uses.
/// </remarks>
internal static class InProcessEventSequence
{
    static readonly FieldInfo _storageField =
        typeof(Grain<Storage.EventSequences.EventSequenceState>)
            .GetField("_storage", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _storage field on Grain<EventSequenceState>.");

    static readonly FieldInfo _eventSequenceKeyField =
        typeof(KernelEventSequences::EventSequence)
            .GetField("_eventSequenceKey", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _eventSequenceKey field on EventSequence.");

    static readonly FieldInfo _eventSequenceIdField =
        typeof(KernelEventSequences::EventSequence)
            .GetField("_eventSequenceId", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find _eventSequenceId field on EventSequence.");

    static readonly FieldInfo _constraintsField =
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
    /// <param name="compliance">The <see cref="InProcessCompliance"/> the scenario shares, so what this grain encrypts on append is what the reading side can release.</param>
    /// <returns>The initialized kernel <see cref="KernelEventSequences::EventSequence"/> grain.</returns>
    internal static async Task<KernelEventSequences::EventSequence> Create(
        InMemoryStorage storage,
        KernelSequenceConcepts::EventSequenceId eventSequenceId,
        KernelConceptsNs::EventStoreName eventStoreName,
        KernelConceptsNs::EventStoreNamespaceName namespaceName,
        InProcessCompliance compliance)
    {
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);

        var eventTypeMigrations = new KernelMigrations::EventTypeMigrations(storage, expandoObjectConverter);
        var eventSerializer = new KernelEventSequences::EventSerializer(
            new InMemoryKernelEventTypes(),
            expandoObjectConverter,
            Cratis.Json.Globals.JsonSerializerOptions ?? new System.Text.Json.JsonSerializerOptions());

        // The Grain base class constructor accesses RuntimeContext.Current!.ObservableLifecycle,
        // which requires a valid IGrainContext to be set as the current execution context.
        // Following the same approach as OrleansTestKit, we set a test context with a lifecycle
        // before constructing the grain, then reset it afterward.
        // The Grain constructor also resolves IGrainRuntime from ActivationServices, so we
        // must provide a service provider with IGrainRuntime registered.
        var grainLifecycle = new TestGrainLifecycle();
        var testServiceProvider = new TestServiceProvider();

        // The grain checks the constraints version on every append (to pick up constraints registered while it is
        // active). In-process there is no constraints grain, so resolve it to a no-op that reports the unset version
        // matching the grain's cached version — the check is a no-op and the injected validators stay in force.
        var runtimeGrainFactory = new InProcessGrainFactory(constraints: new InProcessConstraintsGrain());
        var grainRuntime = new TestGrainRuntime(testServiceProvider, runtimeGrainFactory);
        testServiceProvider.AddService<IGrainRuntime>(grainRuntime);

        var grainContext = new TestGrainContext
        {
            ObservableLifecycle = grainLifecycle,
            ActivationServices = testServiceProvider
        };

        KernelEventSequences::EventSequence grain;

        using (RuntimeContextScope.SetContext(grainContext))
        {
            grain = new KernelEventSequences::EventSequence(
                storage,
                new KernelConstraints::ConstraintValidationFactory(storage),
                eventTypeMigrations,
                null!,
                new ActivitySource<KernelEventSequences::EventSequence>(),
                compliance.Manager,
                expandoObjectConverter,
                eventSerializer,
                new KernelEventSequences::EventHashCalculator(),
                Options.Create(new KernelConfiguration::ChronicleOptions()),
                NullLogger<KernelEventSequences::EventSequence>.Instance,
                NullLogger<KernelEventSequences::Concurrency.ConcurrencyValidator>.Instance);
        }

        var grainStorage = new InMemoryGrainStorage<Storage.EventSequences.EventSequenceState>();
        _storageField.SetValue(grain, grainStorage);

        var key = new KernelSequenceConcepts::EventSequenceKey(eventSequenceId, eventStoreName, namespaceName);
        _eventSequenceKeyField.SetValue(grain, key);
        _eventSequenceIdField.SetValue(grain, eventSequenceId);

        var constraintFactory = new KernelConstraints::ConstraintValidationFactory(storage);
        var constraints = await constraintFactory.Create(key);
        _constraintsField.SetValue(grain, constraints);

        return grain;
    }
}
