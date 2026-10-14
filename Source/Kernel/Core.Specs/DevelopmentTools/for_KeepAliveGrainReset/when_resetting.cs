// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Orleans.Core.Internal;

namespace Cratis.Chronicle.DevelopmentTools.for_KeepAliveGrainReset;

public class when_resetting : Specification
{
    IGrainFactory _grainFactory;
    IAppendedEventsQueue[] _queues;
    IAppendedEventsQueues _router;
    IObserverDevelopmentReset[] _observers;
    IGrainManagementExtension[] _deactivations;
    readonly List<string> _operations = [];

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        var management = Substitute.For<IManagementGrain>();
        _grainFactory.GetGrain<IManagementGrain>(0).Returns(management);
        var queueIds = new[] { GrainId.Create("queue", "0"), GrainId.Create("queue", "1") };
        var routerId = GrainId.Create("queues", "log");
        var observerIds = new[] { GrainId.Create("observer", "one"), GrainId.Create("observer", "two") };
        var queueReference = GrainWithIdentity<IAppendedEventsQueue>(queueIds[0]);
        var routerReference = GrainWithIdentity<IAppendedEventsQueues>(routerId);
        var observerReference = GrainWithIdentity<IObserver>(observerIds[0]);
        _grainFactory.GetGrain<IAppendedEventsQueue>(0, "reset-type-lookup").Returns(queueReference);
        _grainFactory.GetGrain<IAppendedEventsQueues>("reset-type-lookup").Returns(routerReference);
        _grainFactory.GetGrain<IObserver>("reset-type-lookup").Returns(observerReference);
        var ids = queueIds.Concat([routerId]).Concat(observerIds).Append(GrainId.Create("other", "untouched")).ToArray();
        management.GetDetailedGrainStatistics().Returns(ids.Select(id => new DetailedGrainStatistic { GrainId = id }).ToArray());
        _queues = queueIds.Select(id =>
        {
            var queue = Substitute.For<IAppendedEventsQueue>();
            queue.ResetForDevelopment().Returns(_ =>
            {
                _operations.Add("queue-reset");
                return Task.CompletedTask;
            });
            _grainFactory.GetGrain<IAppendedEventsQueue>(id).Returns(queue);
            return queue;
        }).ToArray();
        _router = Substitute.For<IAppendedEventsQueues>();
        _grainFactory.GetGrain<IAppendedEventsQueues>(routerId).Returns(_router);
        _router.ResetForDevelopment().Returns(_ =>
        {
            _operations.Add("router-reset");
            return Task.CompletedTask;
        });
        _observers = observerIds.Select(id =>
        {
            var observer = Substitute.For<IObserverDevelopmentReset>();
            observer.ResetForDevelopment().Returns(_ =>
            {
                _operations.Add("observer-reset");
                return Task.CompletedTask;
            });
            _grainFactory.GetGrain<IObserverDevelopmentReset>(id).Returns(observer);
            return observer;
        }).ToArray();
        _deactivations = queueIds.Append(routerId).Select(id =>
        {
            var deactivation = Substitute.For<IGrainManagementExtension>();
            _grainFactory.GetGrain<IGrainManagementExtension>(id).Returns(deactivation);
            return deactivation;
        }).ToArray();
    }

    async Task Because() => await KeepAliveGrainReset.Reset(_grainFactory);

    static T GrainWithIdentity<T>(GrainId id)
        where T : class, IGrain
    {
        var grain = Substitute.For<T, IGrainBase>();
        var context = Substitute.For<IGrainContext>();
        context.GrainId.Returns(id);
        ((IGrainBase)grain).GrainContext.Returns(context);
        return grain;
    }

    [Fact] void should_reset_every_queue_including_an_orphan() => _queues.ShouldEachConformTo(queue => queue.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAppendedEventsQueue.ResetForDevelopment)) == 1);
    [Fact] async Task should_reset_the_router() => await _router.Received(1).ResetForDevelopment();
    [Fact] void should_seal_every_observer() => _observers.ShouldEachConformTo(observer => observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserverDevelopmentReset.ResetForDevelopment)) == 1);
    [Fact] void should_deactivate_every_queue_and_router() => _deactivations.ShouldEachConformTo(grain => grain.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IGrainManagementExtension.DeactivateOnIdle)) == 1);
    [Fact] void should_stop_delivery_before_sealing_observers() => _operations.ShouldEqual(["queue-reset", "queue-reset", "router-reset", "observer-reset", "observer-reset"]);
}
