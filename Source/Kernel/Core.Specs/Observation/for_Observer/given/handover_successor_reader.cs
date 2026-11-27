// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.given;
using Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;
using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Runs the real successor preparation and page reader on separate test activations, with an explicit boundary between them.
/// </summary>
internal static class handover_successor_reader
{
    internal static Task<IImmutableList<JobStepDetails>> Prepare(CatchUpObserverRequest request, IObserver owner, Key[] keys) =>
        new CatchUpPreparation().Prepare(request, owner, keys);

    internal static Task WalkPages(HandleEventsForObserverArguments request, IObserver owner, IEventSequenceStorage log, IObserverPartitionPageSubscriber subscriber) =>
        new PageWalk().Walk(request, owner, log, subscriber);

    sealed class CatchUpPreparation : a_catch_up_observer_job
    {
        internal async Task<IImmutableList<JobStepDetails>> Prepare(CatchUpObserverRequest request, IObserver owner, Key[] keys)
        {
            await SetUpJob();
            _request = request;
            _keyIndex.GetKeys(request.FromEventSequenceNumber).Returns(CreateKeys(keys));
            _observer.GetSubscription().Returns(_ => owner.GetSubscription());
            _observer.GetFailedPartitionKeys().Returns(_ => owner.GetFailedPartitionKeys());
            _observer.RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>()).Returns(call => owner.RegisterCatchingUpPartitions(call.Arg<IEnumerable<Key>>()));
            await StartAndLetTheStepsCome();
            return _job.PreparedSteps!;
        }
    }

    sealed class PageWalk : a_performing_job_step
    {
        internal async Task Walk(HandleEventsForObserverArguments request, IObserver owner, IEventSequenceStorage log, IObserverPartitionPageSubscriber subscriber)
        {
            await SetUpJob();
            _observer.GetSubscription().Returns(_ => owner.GetSubscription());
            _observer.GetFailedPartitionKeys().Returns(_ => owner.GetFailedPartitionKeys());
            _silo.AddProbe<IObserverPartitionPageSubscriber>(_ => subscriber);
            _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
                .Returns(call => log.GetRange(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSourceId?>(2), call.ArgAt<IEnumerable<EventType>?>(3), call.ArgAt<IEnumerable<Tag>?>(4), call.ArgAt<CancellationToken>(5)));
            _performState.ObserverKey = request.ObserverKey;
            _performState.StartEventSequenceNumber = request.StartEventSequenceNumber;
            _performState.EndEventSequenceNumber = request.EndEventSequenceNumber;
            _performState.EventObservationState = request.EventObservationState;
            _performState.EventTypes = request.EventTypes;
            _performState.ReaderScope = request.ReaderScope;
            _performState.SkipFailedPartitions = request.SkipFailedPartitions;
            await _jobStep.InvokePerformStep(_performState);
        }
    }
}
