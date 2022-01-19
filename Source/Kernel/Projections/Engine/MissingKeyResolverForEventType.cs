// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Exception that gets thrown when there is no <see cref="ValueProvider{Event}"/> providing the key value for a specific <see cref="EventType"/>.
    /// </summary>
    public class MissingKeyResolverForEventType : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingKeyResolverForEventType"/>.
        /// </summary>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        public MissingKeyResolverForEventType(EventType eventType) : base($"Missing key resolver for '{eventType}'")
        {
        }
    }
}
