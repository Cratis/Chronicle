// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;
using Orleans.Streams;
using Orleans.Streams.Core;

namespace Cratis.Events.Store.Observers
{
    /// <summary>
    /// Defines an observer of an event log.
    /// </summary>
    public interface IObserver : IGrainWithGuidCompoundKey
    {
        Task<Guid> Observe();
    }

    [ImplicitStreamSubscription]
    public class Observer : Grain, IObserver, IStreamSubscriptionObserver
    {
        IAsyncStream<AppendedEvent>? _stream;
        EventLogId _eventLogId = EventLogId.Unspecified;
        TenantId _tenantId = TenantId.NotSet;
        IStreamSubscriptionManager _manager;

        public Observer(IStreamSubscriptionManagerAdmin subMan)
        {
            _manager = subMan.GetStreamSubscriptionManager(StreamSubscriptionManagerType.ExplicitSubscribeOnly);
        }

        public override async Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var tenantId);
            _tenantId = tenantId;

            var streamProvider = GetStreamProvider(EventLog.StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());

            var first = new[] { new EventType("9b864474-51eb-4c95-840c-029ee45f3968", EventGeneration.First) };

            // var subscription = await _manager.AddSubscription(
            //     EventLog.StreamProvider,
            //     new StreamIdentity(_eventLogId, _tenantId.ToString()),
            //     GrainReference);

            // var subscriptionHandle = await _stream.SubscribeAsync(
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

            await base.OnActivateAsync();
        }

        public Task<Guid> Observe()
        {
            return Task.FromResult(_stream?.Guid ?? Guid.Empty);
        }

        public Task OnSubscribed(IStreamSubscriptionHandleFactory handleFactory)
        {
            return Task.CompletedTask;
        }
    }
}
