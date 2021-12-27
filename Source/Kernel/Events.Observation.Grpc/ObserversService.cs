// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation.Grpc.Contracts;
using Cratis.Events.Store;
using Cratis.Events.Store.Observers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
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
        readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObserversService"/> class.
        /// </summary>
        /// <param name="logger">Logger for logging.</param>
        /// <param name="getClusterClient"></param>
        /// <param name="serviceProvider"></param>
        public ObserversService(ILogger<ObserversService> logger, GetClusterClient getClusterClient, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _getClusterClient = getClusterClient;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ObserverServerToClient> Subscribe(IAsyncEnumerable<ObserverClientToServer> request, CallContext context = default)
        {
            var client = _getClusterClient();
            var streamProvider = client.GetStreamProvider("event-log");

            var observer = client.GetGrain<IObserver>(EventLogId.Default, keyExtension: "f455c031-630e-450d-a75b-ca050c441708");

            var actualObserver = new ActualObserver(_serviceProvider.GetService<IGrainFactory>()!);
            var obj = await client.CreateObjectReference<IActualObserver>(actualObserver);
            var streamId = await observer.Observe(obj);
            var stream = streamProvider.GetStream<AppendedEvent>(streamId, "f455c031-630e-450d-a75b-ca050c441708");

            var first = new[] { new EventType("9b864474-51eb-4c95-840c-029ee45f3968", EventGeneration.First) };

            // var subscriptionHandle = await stream.SubscribeAsync(
            //     (@event, st) =>
            //     {
            //         Console.WriteLine("Event received");
            //         return Task.CompletedTask;
            //     }, new ObserverStreamSequenceToken(0, first));

            // var resumedSubscriptionHandle = subscriptionHandle.ResumeAsync(
            //     (@event, st) =>
            //     {
            //         Console.WriteLine("Resumed event received");
            //         return Task.CompletedTask;
            //     }, new ObserverStreamSequenceToken(0, first));
            //Console.WriteLine($"{subscriptionHandle.ProviderName} - {subscriptionHandle.HandleId} - {subscriptionHandle.StreamIdentity}");

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

            //await subscriptionHandle.UnsubscribeAsync();

            Console.WriteLine("Unsubsribe");
        }
    }
}
