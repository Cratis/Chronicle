// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Keys;
using Cratis.Observation;

namespace Cratis.Kernel.Observation;

/// <summary>
/// Holds the state for the job to recover a failed partition.
/// </summary>
public class FailedPartition
{
    List<FailedPartitionAttempt> _attempts = [];

    /// <summary>
    /// Unique identifier of the failed partition.
    /// </summary>
    public FailedPartitionId Id { get; set; } = FailedPartitionId.New();

    /// <summary>
    /// Gets or sets the partition that is failed.
    /// </summary>
    public Key Partition { get; set; } = Key.Undefined;

    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> for which this is a failed partition.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets a collection of <see cref="FailedPartitionAttempt"/> for the failed partition.
    /// </summary>
    public IEnumerable<FailedPartitionAttempt> Attempts
    {
        get => _attempts;
        set => _attempts = new(value);
    }

    /// <summary>
    /// Gets or sets whether or not the failure is resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Gets the last attempt for the failed partition.
    /// </summary>
    public FailedPartitionAttempt? LastAttempt => Attempts.LastOrDefault();

    /// <summary>
    /// Add an attempt to the failed partition.
    /// </summary>
    /// <param name="attempt">Attempt to add.</param>
    public void AddAttempt(FailedPartitionAttempt attempt)
    {
        Attempts = Attempts.Append(attempt);
    }
}
