// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Holds well known constants related to grain storage.
/// </summary>
public static class WellKnownGrainStorageProviders
{
    /// <summary>
    /// The name of the storage provider used for namespaces.
    /// </summary>
    public const string Namespaces = "namespaces";

    /// <summary>
    /// The name of the storage provider used for event sequences.
    /// </summary>
    public const string EventSequences = "event-sequences";

    /// <summary>
    /// The name of the storage provider used for observer definitions.
    /// </summary>
    public const string ObserverDefinitions = "observer-definitions";

    /// <summary>
    /// The name of the storage provider used for observers.
    /// </summary>
    public const string ObserverState = "observer-state";

    /// <summary>
    /// The name of the storage provider used for failed partitions on an observer.
    /// </summary>
    public const string FailedPartitions = "failed-partitions";

    /// <summary>
    /// THe name of the storage provider used for jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// THe name of the storage provider used for job steps.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The name of the storage provider used for recommendations.
    /// </summary>
    public const string Recommendations = "recommendations";

    /// <summary>
    /// The name of the storage provider used for projections.
    /// </summary>
    public const string Projections = "projections";

    /// <summary>
    /// The name of the storage provider used for projections manager.
    /// </summary>
    public const string ProjectionsManager = "projections-manager";

    /// <summary>
    /// The name of the storage provider used for projection futures.
    /// </summary>
    public const string ProjectionFutures = "projection-futures";

    /// <summary>
    /// The name of the storage provider used for reactors.
    /// </summary>
    public const string Reactors = "reactors";

    /// <summary>
    /// The name of the storage provider used for webhooks.
    /// </summary>
    public const string Webhooks = "webhooks";

    /// <summary>
    /// The name of the storage provider used for webhooks manager.
    /// </summary>
    public const string WebhooksManager = "webhooks-manager";

    /// <summary>
    /// The name of the storage provider used for reducers.
    /// </summary>
    public const string Reducers = "reducers";

    /// <summary>
    /// The name of the storage provider used for reducers manager.
    /// </summary>
    public const string ReducersManager = "reducers-manager";

    /// <summary>
    /// The name of the storage provider used for constraints.
    /// </summary>
    public const string Constraints = "constraints";

    /// <summary>
    /// The name of the storage provider used for read models.
    /// </summary>
    public const string ReadModels = "read-models";

    /// <summary>
    /// The name of the storage provider used for the read models manager.
    /// </summary>
    public const string ReadModelsManager = "read-models-manager";

    /// <summary>
    /// The name of the storage provider used for event seeding.
    /// </summary>
    public const string EventSeeding = "event-seeding";
}
