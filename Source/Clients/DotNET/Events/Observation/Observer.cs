// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Events.Observation.Grpc.Contracts;
using Cratis.Grpc;
using Newtonsoft.Json;
using ProtoBuf.Grpc;

namespace Cratis.Events.Observation
{

    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    public class Observer : IObserver
    {
        readonly ObserverId _observerId;
        readonly EventLogId _eventLogId;
        readonly IEventTypes _eventTypes;
        readonly IObserverInvoker _observerInvoker;
        readonly IEventSerializer _eventSerializer;
        readonly IGrpcChannel _channel;
        readonly IObserversService _service;

        public Observer(
            ObserverId observerId,
            EventLogId eventLogId,
            IEventTypes eventTypes,
            IObserverInvoker observerInvoker,
            IEventSerializer eventSerializer,
            IGrpcChannel channel)
        {
            _observerId = observerId;
            _eventLogId = eventLogId;
            _eventTypes = eventTypes;
            _observerInvoker = observerInvoker;
            _eventSerializer = eventSerializer;
            _channel = channel;
            _service = _channel.CreateGrpcService<IObserversService>();
        }

        public void StartObserving()
        {
            Task.Run(async () =>
            {
                var stream = Channel.CreateUnbounded<ObserverClientToServer>();
                var result = _service.Subscribe(stream.AsAsyncEnumerable());
                await stream.Writer.WriteAsync(new ObserverClientToServer
                {
                    Subscription = new()
                    {
                        EventLogId = _eventLogId,
                        Id = _observerId
                    }
                });

                await foreach (var request in result)
                {
                    var clrType = _eventTypes.GetClrTypeFor(request.EventTypeId);
                    var content = _eventSerializer.Deserialize(clrType, request.Content);
                    await _observerInvoker.Invoke(content, null!);

                    await stream.Writer.WriteAsync(new ObserverClientToServer
                    {
                        Result = new()
                        {
                            Failed = true,
                            Reason = $"Because we can - {request.Occurred}"
                        }
                    });
                }
            });
        }
    }
}
