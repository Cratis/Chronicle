// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Attribute to adorn types for providing metadata about the actual <see cref="EventType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EventTypeAttribute"/>.
        /// </summary>
        /// <param name="idAsGuid"><see cref="EventTypeId">Identifier</see> as string representation of a <see cref="Guid"/>.</param>
        /// <param name="generation"><see cref="EventGeneration"/> represented as <see cref="uint"/>.</param>
        public EventTypeAttribute(string idAsGuid, uint generation)
        {
            Type = new(Guid.Parse(idAsGuid), generation);
        }

        /// <summary>
        /// Gets the <see cref="EventType"/>.
        /// </summary>
        public EventType Type { get; }
    }
}
