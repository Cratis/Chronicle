// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the key for a job.
/// </summary>
/// <param name="EventStore">The event store the job is for.</param>
/// <param name="Namespace">The namespace within the event store the job is for.</param>
public record JobKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly JobKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="JobKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobKey"/> instance.</returns>
    public static JobKey Parse(string key) => KeyHelper.Parse<JobKey>(key);
}
