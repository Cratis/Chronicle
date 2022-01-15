// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventTypes"/>.
    /// </summary>
    public class EventTypes : IEventTypes
    {
        readonly IDictionary<EventType, Type> _typesByEventType;

        /// <inheritdoc/>
        public IEnumerable<EventType> All { get; }

        /// <summary>
        /// /// Initializes a new instance of <see cref="EventTypes"/>.
        /// </summary>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public EventTypes(ITypes types)
        {
            _typesByEventType = types.All
                            .Where(_ => _.HasAttribute<EventTypeAttribute>())
                            .ToDictionary(_ => _.GetCustomAttribute<EventTypeAttribute>()!.Type!, _ => _);

            All = _typesByEventType.Keys.ToArray();
        }

        /// <inheritdoc/>
        public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

        /// <inheritdoc/>
        public EventType GetEventTypeFor(Type clrType) => _typesByEventType.Single(_ => _.Value == clrType).Key;

        /// <inheritdoc/>
        public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

        /// <inheritdoc/>
        public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType.Single(_ => _.Key.Id == eventTypeId).Value;
    }
}
