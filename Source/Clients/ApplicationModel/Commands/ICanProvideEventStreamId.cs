// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Provides the event stream id.
/// </summary>
public interface ICanProvideEventStreamId
{
    /// <summary>
    /// Gets the event stream id.
    /// </summary>
    /// <returns>The event stream id.</returns>
    EventStreamId GetEventStreamId();
}
