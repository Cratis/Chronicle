// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Receivers;

/// <summary>
/// Defines the provisional route for legacy webhook deliveries.
/// </summary>
internal static class ChronicleReceiverRoute
{
    /// <summary>
    /// The event store route key.
    /// </summary>
    internal const string EventStore = "eventStore";

    /// <summary>
    /// The namespace route key.
    /// </summary>
    internal const string Namespace = "namespace";

    /// <summary>
    /// The observer type route key.
    /// </summary>
    internal const string ObserverType = "observerType";

    /// <summary>
    /// The observer identifier route key.
    /// </summary>
    internal const string ObserverId = "observerId";

    /// <summary>
    /// Creates the provisional target-bearing route pattern.
    /// </summary>
    /// <param name="path">The receiver's base path.</param>
    /// <returns>The route pattern.</returns>
    internal static string Pattern(string path) => "/" + path.Trim('/') + "/{eventStore}/{namespace}/{observerType}/{observerId}";
}
