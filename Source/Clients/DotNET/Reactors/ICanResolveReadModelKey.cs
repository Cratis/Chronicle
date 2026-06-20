// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system that resolves the <see cref="ReadModelKey"/> to use when a reactor handler method
/// takes a read model as a dependency.
/// </summary>
/// <remarks>
/// When a reactor implements this interface, the resolved key is used to materialize every read model
/// parameter on its handler methods. When it is not implemented, the <see cref="EventContext.EventSourceId"/>
/// of the triggering event is used as the key.
/// </remarks>
public interface ICanResolveReadModelKey
{
    /// <summary>
    /// Resolve the <see cref="ReadModelKey"/> to use for materializing read model dependencies.
    /// </summary>
    /// <param name="event">The deserialized event that triggered the reactor.</param>
    /// <param name="context">The <see cref="EventContext"/> for the triggering event.</param>
    /// <returns>The <see cref="ReadModelKey"/> to materialize read model dependencies with.</returns>
    ReadModelKey Resolve(object @event, EventContext context);
}
