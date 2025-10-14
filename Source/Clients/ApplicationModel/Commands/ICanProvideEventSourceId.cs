// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Provides the event source id.
/// </summary>
public interface ICanProvideEventSourceId
{
    /// <summary>
    /// Gets the event source id.
    /// </summary>
    /// <returns>The event source id.</returns>
    EventSourceId GetEventSourceId();
}
