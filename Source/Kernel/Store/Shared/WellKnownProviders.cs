// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Holds well known constants related to observation.
/// </summary>
public static class WellKnownProviders
{
    /// <summary>
    /// The name of the stream provider used for observer handlers.
    /// </summary>
    public const string ObserverHandlersStreamProvider = "observer-handlers";

    /// <summary>
    /// The name of the stream provider.
    /// </summary>
    public const string EventSequenceStreamProvider = "event-sequence";
}
