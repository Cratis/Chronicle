// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;
using Orleans.Runtime;
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
        void OnNext(AppendedEvent @event, Guid id, string tenantId);
    }

    public class ActualObserver : IActualObserver
    {
        readonly IGrainFactory _grainFactory;

        public ActualObserver(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public void OnNext(AppendedEvent @event, Guid id, string tenantId)
        {
            var grain = _grainFactory.GetGrain<IPartitionedObserver>(id, keyExtension: tenantId);
            grain.ReportStatus().Wait();

            Console.WriteLine("Do stuff");
        }
    }


    public interface IPartitionedObserver : IGrainWithGuidCompoundKey
    {
        Task<bool> OnNext(IActualObserver observer, AppendedEvent @event);
        Task ReportStatus();
    }

    public class PartitionedObserver : Grain, IPartitionedObserver
    {
        public Task<bool> OnNext(IActualObserver observer, AppendedEvent @event)
        {
            var id = this.GetPrimaryKey(out var tenantId);

            observer.OnNext(@event, id, tenantId);

            return Task.FromResult(true);
        }

        public Task ReportStatus()
        {
            return Task.CompletedTask;
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

            await base.OnActivateAsync();
        }

        public async Task<Guid> Observe(IActualObserver actualObserver)
        {
            var first = new[] { new EventType("9b864474-51eb-4c95-840c-029ee45f3968", EventGeneration.First) };
            var subscriptionHandle = await _stream!.SubscribeAsync(
                async (@event, st) =>
                {
                    var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(Guid.Parse("9fd4ce1a-1d68-439c-9e4b-0edd45e00445"), keyExtension: @event.EventContext.EventSourceId);
                    await partitionedObserver.OnNext(actualObserver, @event);

                    Console.WriteLine("Event received");
                    // try
                    // {
                    //     actualObserver.OnNext();
                    // }
                    // catch (Exception ex)
                    // {
                    //     Console.WriteLine($"Observer failed - {ex.Message} - {ex.StackTrace}");
                    // }
                }, new ObserverStreamSequenceToken(0, first));

            return Guid.Empty;
        }
    }
}
