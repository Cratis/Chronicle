// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Identifies what serves an observer's subscription, as a closed set derived from the subscriber type.
/// </summary>
/// <remarks>
/// The CLR subscriber type is never stored; it is derived from the kind. Decision 0034, point 1.
/// </remarks>
public enum ObserverSubscriptionKind
{
    /// <summary>
    /// No kind. Never stored: an observer without a durable subscription has none.
    /// </summary>
    None = 0,

    /// <summary>
    /// A reactor served by a connected client.
    /// </summary>
    ClientReactor = 1,

    /// <summary>
    /// A reducer served by a connected client.
    /// </summary>
    ClientReducer = 2,

    /// <summary>
    /// A projection served by the kernel.
    /// </summary>
    Projection = 3,

    /// <summary>
    /// A reactor implemented inside the kernel.
    /// </summary>
    KernelReactor = 4,

    /// <summary>
    /// A webhook.
    /// </summary>
    Webhook = 5,

    /// <summary>
    /// An event store subscription.
    /// </summary>
    EventStoreSubscription = 6
}
