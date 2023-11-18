// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Holds well known constants related to grain storage.
/// </summary>
public static class WellKnownGrainStorageProviders
{
    /// <summary>
    /// The name of the storage provider used for event sequences.
    /// </summary>
    public const string EventSequences = "event-sequence";

    /// <summary>
    /// The name of the storage provider used for observers.
    /// </summary>
    public const string Observers = "observer";

    /// <summary>
    /// The name of the storage provider used for failed partitions on an observer.
    /// </summary>
    public const string FailedPartitions = "failed-partitions";

    /// <summary>
    /// The name of the storage provider used for tenant configuration.
    /// </summary>
    public const string TenantConfiguration = "tenant-configuration";

    /// <summary>
    /// THe name of the storage provider used for jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// THe name of the storage provider used for jobs.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The name of the storage provider used for operations.
    /// </summary>
    public const string Operations = "operations";
}
