// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;
using Orleans.Streams;

namespace Cratis.Events.Store.Observers
{
    /// <summary>
    /// Defines an observer of an event log.
    /// </summary>
    public interface IObserver : IGrainWithGuidCompoundKey
    {
        Task<Guid> Observe(IActualObserver actualObserver);
    }

    public interface IActualObserver : IGrainObserver
    {
        void DoStuff();
    }

    public class ActualObserver : IActualObserver
    {
        public void DoStuff()
        {
            Console.WriteLine("Do stuff");
            throw new ArgumentException("Hello");
        }
    }


    public class Observer : Grain, IObserver
    {
        IAsyncStream<AppendedEvent>? _stream = null;
        EventLogId _eventLogId = EventLogId.Unspecified;
        TenantId _tenantId = TenantId.NotSet;

        public override async Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var tenantId);
            _tenantId = tenantId;

            var streamProvider = GetStreamProvider(EventLog.StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());


            // var subscription = await _manager.AddSubscription(
            //     EventLog.StreamProvider,
            //     new StreamIdentity(_eventLogId, _tenantId.ToString()),
            //     GrainReference);

            await base.OnActivateAsync();
        }

        public async Task<Guid> Observe(IActualObserver actualObserver)
        {
            var first = new[] { new EventType("9b864474-51eb-4c95-840c-029ee45f3968", EventGeneration.First) };
            var subscriptionHandle = await _stream.SubscribeAsync(
                (@event, st) =>
                {
                    Console.WriteLine("Event received");
                    try
                    {
                        actualObserver.DoStuff();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Observer failed - {ex.Message} - {ex.StackTrace}");

                    }
                    return Task.CompletedTask;
                }, new ObserverStreamSequenceToken(0, first));

            // var resumedSubscriptionHandle = subscriptionHandle.ResumeAsync(
            //     (@event, st) =>
            //     {
            //         Console.WriteLine("Resumed event received");
            //         return Task.CompletedTask;
            //     }, new ObserverStreamSequenceToken(0, first));

            return Guid.Empty;
        }
    }
}
