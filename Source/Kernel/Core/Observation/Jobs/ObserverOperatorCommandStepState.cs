// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the state of the <see cref="ObserverOperatorCommandStep"/>.
/// </summary>
/// <remarks>
/// The observer is kept as plain values.
/// </remarks>
public class ObserverOperatorCommandStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the name of the event store of the observer.
    /// </summary>
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the namespace of the observer.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the observer the command is for.
    /// </summary>
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the event sequence the observer observes.
    /// </summary>
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to carry out.
    /// </summary>
    public ObserverOperatorCommand Command { get; set; }

    /// <summary>
    /// Gets or sets the partition the command is for, for the commands that are for one partition; otherwise empty.
    /// </summary>
    public string Partition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether clearing the quarantine of a partition also retries it.
    /// </summary>
    public bool RetryImmediately { get; set; }
}
