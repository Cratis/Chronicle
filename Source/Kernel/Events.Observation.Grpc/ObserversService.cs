// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation.Grpc.Contracts;
using ProtoBuf.Grpc;

namespace Cratis.Events.Observation.Grpc
{
    public class ObserversService : IObserversService
    {
        public async IAsyncEnumerable<ObserverServerToClient> Subscribe(IAsyncEnumerable<ObserverClientToServer> request, CallContext context = default)
        {
            Console.WriteLine("Subscribe");

            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);

                var enumerator = request.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync())
                {
                    var observerRequest = enumerator.Current;
                    if (observerRequest.Subscription != null)
                    {
                        Console.WriteLine($"Subscriber {observerRequest.Subscription.Id} - {observerRequest.Subscription.EventLogId}");
                    }
                    else
                    {
                        Console.WriteLine($"Result - {observerRequest.Result.Failed} - {observerRequest.Result.Reason}");
                    }
                    break;
                }
                yield return new ObserverServerToClient
                {
                    EventTypeId = Guid.Parse("8a109d92-aa6a-4aee-8852-f8f2528da1fc"),
                    Occurred = DateTime.UtcNow,
                    Content = "{ \"ma,e\": \"Hello world\", \"owner\": \"94c7de3b-732a-4843-a764-db885bbdb360\" }"
                };
            }

            Console.WriteLine("Unsubsribe");
        }
    }
}
