// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Resolves which events the local consumer code wants from a binding's logical source: the consumer decides, the producer only intersects with its grant.
/// </summary>
public interface IFederationSubscriptions
{
    /// <summary>
    /// Get the event store subscriptions of the binding's target event store that name the binding's logical source.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <returns>The subscriptions, empty when nothing subscribes yet.</returns>
    Task<IReadOnlyList<FederationSubscription>> Get(FederationBindingState binding);
}
