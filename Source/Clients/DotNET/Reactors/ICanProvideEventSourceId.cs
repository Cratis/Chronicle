// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a reactor (or any object) that can provide the <see cref="EventSourceId"/> to use when
/// appending side-effect events returned from handler methods.
/// </summary>
/// <remarks>
/// Implement this interface on a reactor class to override the default behavior, which uses the
/// <see cref="EventSourceId"/> from the incoming <see cref="EventContext"/>.
/// </remarks>
public interface ICanProvideEventSourceId
{
    /// <summary>
    /// Gets the <see cref="EventSourceId"/> to use when appending side-effect events.
    /// </summary>
    /// <returns>The <see cref="EventSourceId"/>.</returns>
    EventSourceId GetEventSourceId();
}
