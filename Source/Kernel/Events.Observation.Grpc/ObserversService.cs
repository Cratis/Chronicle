// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation.Grpc.Contracts;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Events.Observation.Grpc
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserversService"/>.
    /// </summary>
    public class ObserversService : IObserversService
    {
        readonly ILogger<ObserversService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObserversService"/> class.
        /// </summary>
        /// <param name="logger">Logger for logging.</param>
        public ObserversService(ILogger<ObserversService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
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
                        Console.WriteLine($"Subscriber {observerRequest.Subscription.Id} - {observerRequest.Subscription.Name} - {observerRequest.Subscription.EventLogId}");
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
