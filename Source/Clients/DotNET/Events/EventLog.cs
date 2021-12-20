// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Grpc.Contracts;
using Cratis.Grpc;
using Newtonsoft.Json;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : IEventLog
    {
        readonly IGrpcChannel _channel;
        readonly EventLogId _eventLogId;

        readonly IEventLogService _eventLogService;

        public EventLog(IGrpcChannel channel, EventLogId eventLogId)
        {
            _channel = channel;
            _eventLogId = eventLogId;
            _eventLogService = _channel.CreateGrpcService<IEventLogService>();
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, object content)
        {
            var request = new AppendRequest
            {
                EventLogId = _eventLogId,
                EventSourceId = eventSourceId,
                EventType = new()
                {
                    EventTypeId = Guid.Parse("e4139473-287f-4565-a396-e998b4421e25"),
                    Generation = 1
                },
                Content = JsonConvert.SerializeObject(content)
            };

            var response = await _eventLogService.Append(request);
            Console.WriteLine($"Result : {response.Success}");
        }
    }
}
