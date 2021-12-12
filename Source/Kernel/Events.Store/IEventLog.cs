// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8019  // TODO: Orleans CodeGenerator is failing due to this: https://www.ingebrigtsen.info/2021/08/13/orleans-and-c-10-global-usings/
using System.Threading.Tasks;
using Orleans;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Defines an immutable event log.
    /// </summary>
    public interface IEventLog : IGrainWithGuidCompoundKey
    {
        /// <summary>
        /// Commit a single event to the event store.
        /// </summary>
        /// <param name="eventSourceId">The <see cref="EventSourceId"/> to commit for.</param>
        /// <param name="eventType">The <see cref="EventType">type of event</see> to commit.</param>
        /// <param name="content">The JSON payload of the event.</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        Task Commit(EventSourceId eventSourceId, EventType eventType, string content);

        /// <summary>
        /// Compensate a specific event in the event store.
        /// </summary>
        /// <param name="sequenceNumber">The <see cref="EventLogSequenceNumber"/> of the event to compensate.</param>
        /// <param name="eventType">The <see cref="EventType">type of event</see> to compensate.</param>
        /// <param name="content">The JSON payload of the event.</param>
        /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        /// <remarks>
        /// The type of the event has to be the same as the original event at the sequence number.
        /// Its generational information is taken into account when compensating.
        /// </remarks>
        Task Compensate(EventLogSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default);
    }
}
