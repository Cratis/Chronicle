// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Internal class for holding all collection names.
/// </summary>
public static class WellKnownCollectionNames
{
    /// <summary>
    /// The collection that holds <see cref="EventStore"/>.
    /// </summary>
    public const string EventStores = "event-stores";

    /// <summary>
    /// The collection that holds <see cref="Event"/>.
    /// </summary>
    public const string EventLog = "event-log";

    /// <summary>
    /// The collection that holds all namespaces.
    /// </summary>
    public const string Namespaces = "namespaces";

    /// <summary>
    /// The collection that holds <see cref="Event"/> for the system.
    /// </summary>
    public const string System = "system";

    /// <summary>
    /// The collection that holds observer state.
    /// </summary>
    public const string Observers = "observers";

    /// <summary>
    /// The collection that holds event sequences.
    /// </summary>
    public const string EventSequences = "event-sequences";

    /// <summary>
    /// The collection that holds connected clients state.
    /// </summary>
    public const string ConnectedClients = "connected-clients";

    /// <summary>
    /// The collection that holds schemas.
    /// </summary>
    public const string Schemas = "schemas";

    /// <summary>
    /// The collection that holds failed partitions.
    /// </summary>
    public const string FailedPartitions = "failed-partitions";

    /// <summary>
    /// The collection that holds identities.
    /// </summary>
    public const string Identities = "identities";

    /// <summary>
    /// The collection that holds jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// The collection that holds job steps.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The collection that holds failed job steps.
    /// </summary>
    public const string FailedJobSteps = "failed-job-steps";

    /// <summary>
    /// The collection that holds recommendations.
    /// </summary>
    public const string Recommendations = "recommendations";

    /// <summary>
    /// The collection that holds reducer definitions.
    /// </summary>
    public const string ReactorDefinitions = "reactor-definitions";

    /// <summary>
    /// The collection that holds reducer definitions.
    /// </summary>
    public const string ReducerDefinitions = "reducer-definitions";

    /// <summary>
    /// The collection that holds projection definitions.
    /// </summary>
    public const string ProjectionDefinitions = "projection-definitions";

    /// <summary>
    /// The collection that holds the definitions of constraints.
    /// </summary>
    public const string Constraints = "constraints";

    /// <summary>
    /// The collection that holds replay contexts.
    /// </summary>
    public const string ReplayContexts = "replay-contexts";

    /// <summary>
    /// The collection that holds replayed models.
    /// </summary>
    public const string ReplayedModels = "replayed-models";
}
