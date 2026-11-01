// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Local diagnostic only. The startup operations a <see cref="StartupDiagnosticRecorder"/> can attribute.
/// </summary>
internal enum StartupOperation
{
    /// <summary>
    /// First client artifacts provider initialization.
    /// </summary>
    ProviderInitialization = 1,

    /// <summary>
    /// Assembly discovery within provider initialization.
    /// </summary>
    AssemblyDiscovery = 2,

    /// <summary>
    /// Precise event schema generation call.
    /// </summary>
    GenerateEventSchema = 3,

    /// <summary>
    /// Read model schema generation call.
    /// </summary>
    GenerateReadModelSchema = 4,

    /// <summary>
    /// Legacy event schema generation call.
    /// </summary>
    GenerateLegacySchema = 5,

    /// <summary>
    /// Execution of the precise event schema cache factory.
    /// </summary>
    EventSchemaFactory = 6,

    /// <summary>
    /// Execution of the read model schema cache factory.
    /// </summary>
    ReadModelSchemaFactory = 7,

    /// <summary>
    /// Execution of the legacy event schema cache factory.
    /// </summary>
    LegacySchemaFactory = 8,

    /// <summary>
    /// Execution of the precise structure cache factory.
    /// </summary>
    PreciseStructureFactory = 9,

    /// <summary>
    /// Execution of the legacy structure cache factory.
    /// </summary>
    LegacyStructureFactory = 10,

    /// <summary>
    /// Registry binding of a structural schema.
    /// </summary>
    RegistryBinding = 11,

    /// <summary>
    /// Event types registration payload preparation.
    /// </summary>
    EventTypesPreparation = 12,

    /// <summary>
    /// Event types registration transport.
    /// </summary>
    EventTypesSend = 13,

    /// <summary>
    /// Event store registration preparation.
    /// </summary>
    StorePreparation = 14,

    /// <summary>
    /// The whole event store artifact registration.
    /// </summary>
    StoreRegistration = 15,

    /// <summary>
    /// Parallel registration of event types, event sources and read models.
    /// </summary>
    ParallelTypes = 16,

    /// <summary>
    /// Parallel registration of constraints, observers and projections.
    /// </summary>
    ParallelObservers = 17,

    /// <summary>
    /// Registration of external subscriptions.
    /// </summary>
    Subscriptions = 18,

    /// <summary>
    /// Event seeding registration.
    /// </summary>
    Seeding = 19
}
