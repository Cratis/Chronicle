// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a key for a <see cref="IJobsManager"/>.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
public record JobsManagerKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Gets the not set <see cref="JobsManagerKey"/>.
    /// </summary>
    public static readonly JobsManagerKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="JobsManagerKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="JobsManagerKey"/> to convert from.</param>
    public static implicit operator string(JobsManagerKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from string to <see cref="JobsManagerKey"/>.
    /// </summary>
    /// <param name="key">String representation to convert from.</param>
    public static implicit operator JobsManagerKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="JobsManagerKey"/> instance.</returns>
    public static JobsManagerKey Parse(string key)
    {
        var elements = key.Split('+');
        var eventStore = (EventStoreName)elements[0];
        var @namespace = (EventStoreNamespaceName)elements[1];
        return new JobsManagerKey(eventStore, @namespace);
    }
}
