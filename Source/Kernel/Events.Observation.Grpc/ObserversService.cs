// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation.Grpc.Contracts;
using Cratis.Events.Store;
using Cratis.Events.Store.Observers;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using ProtoBuf.Grpc;

namespace Cratis.Events.Observation.Grpc
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserversService"/>.
    /// </summary>
    public class ObserversService : IObserversService
    {
        readonly ILogger<ObserversService> _logger;
        readonly GetClusterClient _getClusterClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObserversService"/> class.
        /// </summary>
        /// <param name="logger">Logger for logging.</param>
        /// <param name="getClusterClient"></param>
        public ObserversService(ILogger<ObserversService> logger, GetClusterClient getClusterClient)
        {
            _logger = logger;
            _getClusterClient = getClusterClient;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ObserverServerToClient> Subscribe(IAsyncEnumerable<ObserverClientToServer> request, CallContext context = default)
        {
            var streamProvider = _getClusterClient().GetStreamProvider("event-log");
            var stream = streamProvider.GetStream<AppendedEvent>(Guid.Empty, "f455c031-630e-450d-a75b-ca050c441708");

            var first = new[] { new EventType("9b864474-51eb-4c95-840c-029ee45f3968", EventGeneration.First) };
            var second = new[] { new EventType("90882b74-a5a5-47c7-aabe-f19926080bd0", EventGeneration.First) };
            var third = new[] { new EventType("9f60c9ce-136e-4609-8ed1-ab5cabdf6128", EventGeneration.First) };

            var subscriptionHandle = await stream.SubscribeAsync(
                (@event, st) =>
                {
                    Console.WriteLine("Event received");
                    return Task.CompletedTask;
                }, new ObserverStreamSequenceToken(0, first));
            Console.WriteLine($"{subscriptionHandle.ProviderName} - {subscriptionHandle.HandleId} - {subscriptionHandle.StreamIdentity}");

            subscriptionHandle = await stream.SubscribeAsync(
                (@event, st) =>
                {
                    Console.WriteLine("Event received");
                    return Task.CompletedTask;
                }, new ObserverStreamSequenceToken(0, second));
            Console.WriteLine($"{subscriptionHandle.ProviderName} - {subscriptionHandle.HandleId} - {subscriptionHandle.StreamIdentity}");

            subscriptionHandle = await stream.SubscribeAsync(
                (@event, st) =>
                {
                    Console.WriteLine("Event received");
                    return Task.CompletedTask;
                }, new ObserverStreamSequenceToken(0, third));
            Console.WriteLine($"{subscriptionHandle.ProviderName} - {subscriptionHandle.HandleId} - {subscriptionHandle.StreamIdentity}");

            while (!context.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), context.CancellationToken);
                }
                catch { break; }

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

            await subscriptionHandle.UnsubscribeAsync();

            Console.WriteLine("Unsubsribe");
        }
    }
}
