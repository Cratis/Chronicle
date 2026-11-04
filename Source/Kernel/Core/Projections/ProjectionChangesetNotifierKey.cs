// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the key for <see cref="IProjectionChangesetNotifier"/>, scoping one notifier to an observer within an
/// event store and namespace.
/// </summary>
/// <param name="ObserverId">The unique identifier of the observer producing the changes.</param>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
public record ProjectionChangesetNotifierKey(ObserverId ObserverId, EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionChangesetNotifierKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ProjectionChangesetNotifierKey"/> to convert from.</param>
    public static implicit operator string(ProjectionChangesetNotifierKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(ObserverId, EventStore, Namespace);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionChangesetNotifierKey"/> instance.</returns>
    public static ProjectionChangesetNotifierKey Parse(string key) => KeyHelper.Parse<ProjectionChangesetNotifierKey>(key);
}
