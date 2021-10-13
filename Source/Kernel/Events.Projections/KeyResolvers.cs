// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a helper class for creating <see cref="KeyResolver"/> delegates.
    /// </summary>
    public class KeyResolvers
    {
        /// <summary>
        /// Gets the <see cref="KeyResolver"/> for <see cref="EventSourceId"/>.
        /// </summary>
        public static readonly KeyResolver   EventSourceId = (@event) => @event.EventSourceId.Value;
    }
}
