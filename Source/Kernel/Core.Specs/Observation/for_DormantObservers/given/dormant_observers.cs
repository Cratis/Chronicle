// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_DormantObservers.given;

public class dormant_observers : Specification
{
    protected static readonly ObserverKey _observer = new("the-observer", "the-event-store", "the-namespace", EventSequenceId.Log);
    protected static readonly ObserverDurableSubscription _record = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("the-connection", "silo")]);

    protected IGrainFactory _grainFactory;
    protected IAppendedEventsQueues _queues;
    protected IDurableSubscriptionRecords _records;
    protected DormantObservers _dormantObservers;
    protected ObserverDurableSubscription? _result;

    void Establish()
    {
        _queues = Substitute.For<IAppendedEventsQueues>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IAppendedEventsQueues>(Arg.Any<string>(), Arg.Any<string>()).Returns(_queues);
        _records = Substitute.For<IDurableSubscriptionRecords>();
        _records.CanBeRelied.Returns(true);
        _records.Get(_observer).Returns(Task.FromResult<ObserverDurableSubscription?>(_record));
        _dormantObservers = new(_grainFactory, _records);
    }
}
