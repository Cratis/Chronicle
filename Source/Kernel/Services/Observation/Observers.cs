// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Aksio.Cratis.Kernel.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Aksio.Cratis.Kernel.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : IObservers
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ObserverClientMessage> messages, CallContext context = default)
    {
        var registerTcs = new TaskCompletionSource();
        var tcs = new TaskCompletionSource();
        RegisterObserver registration;

        messages.Subscribe(message =>
        {
            switch (message.Content.Value)
            {
                case RegisterObserver register:
                    Console.WriteLine($"Registering observer: {register.ObserverName}");
                    registration = register;
                    registerTcs.SetResult();
                    break;

                case ObservationResult result:
                    Console.WriteLine($"Result: {result.State}");
                    tcs.SetResult();
                    break;
            }
        });

        return Observable.Create<EventsToObserve>(async (observer, cancellationToken) =>
        {
            try
            {
                await registerTcs.Task;

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    observer.OnNext(new EventsToObserve());

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    tcs = new TaskCompletionSource();
                }
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <inheritdoc/>
    public Task RetryPartition(RetryPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Rewind(RewindRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task RewindPartition(RewindPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<ObserverInformation> GetObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObservable<ObserverInformation> AllObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();
}
