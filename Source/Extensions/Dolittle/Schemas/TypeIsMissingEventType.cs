// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Reflection;
using Dolittle.SDK.Events;

namespace Cratis.Extensions.Dolittle.Schemas
{
    /// <summary>
    /// Exception that gets thrown when a <see cref="Type"/> does not have any <see cref="EventType"/> information available.
    /// </summary>
    public class TypeIsMissingEventType : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeIsMissingEventType"/> class.
        /// </summary>
        /// <param name="type">Type that is missing information.</param>
        public TypeIsMissingEventType(Type type)
            : base($"Type '{type.AssemblyQualifiedName}' does not have any event type information associated with it.")
        {
        }

        /// <summary>
        /// Throw <see cref="TypeIsMissingEventType"/> if <see cref="EventType"/> is not available.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to check.</param>
        /// <exception cref="TypeIsMissingEventType">Thrown if <see cref="EventType"/> is not available.</exception>
        public static void ThrowIfMissingEventType(Type type)
        {
            if (!type.HasAttribute<EventTypeAttribute>())
            {
                throw new TypeIsMissingEventType(type);
            }
        }
    }
}
