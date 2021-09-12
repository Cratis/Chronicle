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
    }
}
