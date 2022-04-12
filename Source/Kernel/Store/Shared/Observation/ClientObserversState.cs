// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents the state used for tracking client observers.
/// </summary>
public class ClientObserversState
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "client-observers-state";

    /// <summary>
    /// Gets or sets the identifier of the client observers state.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observer keys grouped per connection.
    /// </summary>
    public IDictionary<string, List<ObserverFullyQualifiedIdentifier>> ObserverKeysByConnections { get; set; } = new Dictionary<string, List<ObserverFullyQualifiedIdentifier>>();

    /// <summary>
    /// Check for a specific connection identifier.
    /// </summary>
    /// <param name="connectionId">Identifier to check for.</param>
    /// <returns>True if the connection is registered, false if not.</returns>
    public bool HasConnectionId(string connectionId) => ObserverKeysByConnections.ContainsKey(connectionId);

    /// <summary>
    /// Gets the collection of <see cref="ObserverKey"/> associated with a connection identifier.
    /// </summary>
    /// <param name="connectionId">Identifier to get for.</param>
    /// <returns>Collection of <see cref="ObserverKey"/>.</returns>
    public IEnumerable<ObserverFullyQualifiedIdentifier> GetObserversForConnectionId(string connectionId) => ObserverKeysByConnections[connectionId];

    /// <summary>
    /// Associate observer with a specific connection.
    /// </summary>
    /// <param name="connectionId">Connection to associate with.</param>
    /// <param name="observerKey">Observer key to associate.</param>
    public void AssociateObserverWithConnectionId(string connectionId, ObserverFullyQualifiedIdentifier observerKey)
    {
        if (!ObserverKeysByConnections.ContainsKey(connectionId))
        {
            ObserverKeysByConnections[connectionId] = new();
        }

        var observerKeys = ObserverKeysByConnections[connectionId];
        if (observerKeys.Contains(observerKey))
        {
            return;
        }

        observerKeys.Add(observerKey);
    }
}
