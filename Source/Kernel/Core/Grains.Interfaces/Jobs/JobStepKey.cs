// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents the key for a job step.
/// </summary>
/// <param name="JobId">The job the step is for.</param>
/// <param name="EventStore">The event store the job step is for.</param>
/// <param name="Namespace">The namespace within the event store the job step is for.</param>
public record JobStepKey(JobId JobId, EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="JobKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobStepKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobStepKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(JobId, EventStore, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobKey"/> instance.</returns>
    public static JobStepKey Parse(string key) => KeyHelper.Parse<JobStepKey>(key);
}
