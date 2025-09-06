// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Internal class for holding all table names.
/// </summary>
public static class WellKnownTableNames
{
    /// <summary>
    /// The table that holds <see cref="EventStore"/>.
    /// </summary>
    public const string EventStores = "EventStores";

    /// <summary>
    /// The table that holds evens for the event log sequence.
    /// </summary>
    public const string EventLog = "EventLog";

    /// <summary>
    /// The table that holds all namespaces.
    /// </summary>
    public const string Namespaces = "namespaces";

    /// <summary>
    /// The table that holds events for the system.
    /// </summary>
    public const string System = "System";

    /// <summary>
    /// The table that holds observer state.
    /// </summary>
    public const string Observers = "Observers";

    /// <summary>
    /// The table that holds event sequences.
    /// </summary>
    public const string EventSequences = "EventSequences";

    /// <summary>
    /// The table that holds connected clients state.
    /// </summary>
    public const string ConnectedClients = "ConnectedClients";

    /// <summary>
    /// The table that holds event types.
    /// </summary>
    public const string EventTypes = "EventTypes";

    /// <summary>
    /// The table that holds failed partitions.
    /// </summary>
    public const string FailedPartitions = "FailedPartitions";

    /// <summary>
    /// The table that holds identities.
    /// </summary>
    public const string Identities = "Identities";

    /// <summary>
    /// The table that holds jobs.
    /// </summary>
    public const string Jobs = "Jobs";

    /// <summary>
    /// The table that holds job steps.
    /// </summary>
    public const string JobSteps = "JobSteps";

    /// <summary>
    /// The table that holds failed job steps.
    /// </summary>
    public const string FailedJobSteps = "FailedJobSteps";

    /// <summary>
    /// The table that holds recommendations.
    /// </summary>
    public const string Recommendations = "Recommendations";

    /// <summary>
    /// The table that holds reducer definitions.
    /// </summary>
    public const string ReactorDefinitions = "Reactors";

    /// <summary>
    /// The table that holds reducer definitions.
    /// </summary>
    public const string ReducerDefinitions = "Reducer";

    /// <summary>
    /// The table that holds projection definitions.
    /// </summary>
    public const string ProjectionDefinitions = "Projections";

    /// <summary>
    /// The table that holds the definitions of constraints.
    /// </summary>
    public const string Constraints = "Constraints";

    /// <summary>
    /// The table that holds replay contexts.
    /// </summary>
    public const string ReplayContexts = "ReplayContexts";

    /// <summary>
    /// The table that holds replayed read models.
    /// </summary>
    public const string ReplayedReadModels = "ReplayedReadModels";

    /// <summary>
    /// The table that holds read model definitions.
    /// </summary>
    public const string ReadModelDefinitions = "ReadModels";
}
