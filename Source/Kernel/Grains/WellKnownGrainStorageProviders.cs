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
    /// The name of the storage provider used for observers.
    /// </summary>
    public const string Observers = "observers";

    /// <summary>
    /// The name of the storage provider used for failed partitions on an observer.
    /// </summary>
    public const string FailedPartitions = "failed-partitions";

    /// <summary>
    /// THe name of the storage provider used for jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// THe name of the storage provider used for jobs.
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
    /// The name of the storage provider used for projection-manager.
    /// </summary>
    public const string ProjectionManager = "projection-manager";

    /// <summary>
    /// The name of the storage provider used for projection subscribers.
    /// </summary>
    public const string ProjectionSubscribers = "projection-subscribers";
}
