// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;
using Cratis.Events.Store.Observation;
using Cratis.Events.Store.Grains.Observation;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    public class ObserverHandler : IObserverHandler
    {
        readonly IEventTypes _eventTypes;
        readonly IObserverInvoker _observerInvoker;
        readonly IEventSerializer _eventSerializer;

        public ObserverHandler(
            ObserverId observerId,
            ObserverName name,
            EventLogId eventLogId,
            IEventTypes eventTypes,
            IObserverInvoker observerInvoker,
            IEventSerializer eventSerializer)
        {
            ObserverId = observerId;
            Name = name;
            EventLogId = eventLogId;
            _eventTypes = eventTypes;
            _observerInvoker = observerInvoker;
            _eventSerializer = eventSerializer;
        }

        public ObserverId ObserverId { get; }
        public ObserverName Name { get; }
        public EventLogId EventLogId { get; }


        public void OnNext(ObserverContext context, AppendedEvent @event)
        {
            var i=0;
            i++;

        }

        public void StartObserving()
        {
            Task.Run(() =>
            {
                // var stream = Channel.CreateUnbounded<ObserverClientToServer>();
                // var result = _service.Subscribe(stream.AsAsyncEnumerable());
                // await stream.Writer.WriteAsync(new ObserverClientToServer
                // {
                //     Subscription = new()
                //     {
                //         EventLogId = _eventLogId,
                //         Id = _observerId,
                //         Name = _name
                //     }
                // });

                // await foreach (var request in result)
                // {
                //     ObserverClientToServer response;
                //     try
                //     {
                //         var clrType = _eventTypes.GetClrTypeFor(request.EventTypeId);
                //         var content = _eventSerializer.Deserialize(clrType, request.Content);
                //         await _observerInvoker.Invoke(content, null!);
                //         response = new()
                //         {
                //             Result = new()
                //             {
                //                 Failed = false,
                //                 Reason = string.Empty
                //             }
                //         };
                //     }
                //     catch (Exception ex)
                //     {
                //         response = new()
                //         {
                //             Result = new()
                //             {
                //                 Failed = true,
                //                 Reason = ex.Message
                //             }
                //         };
                //     }

                //     await stream.Writer.WriteAsync(response);
                // }
            });
        }
    }
}
