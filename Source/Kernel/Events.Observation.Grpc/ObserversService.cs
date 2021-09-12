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
                    EventTypeId = Guid.Parse("bca45a99-5f56-4f4e-acf7-f086de4dd72b"),
                    Occurred = DateTime.UtcNow,
                    Content = "{ \"theString\": \"Hello world\", \"theInt\": 42 }"
                };
            }

            Console.WriteLine("Unsubsribe");
        }
    }
}
