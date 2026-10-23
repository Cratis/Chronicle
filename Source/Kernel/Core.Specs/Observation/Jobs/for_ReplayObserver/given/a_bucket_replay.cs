// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.given;

public class a_bucket_replay : a_replay_observer_job
{
    protected virtual int BucketCount => 8;
    protected virtual bool GuardWrites => true;
    protected virtual bool Collapsing => false;
    protected virtual bool Independent => true;

    protected override ChronicleOptions CreateOptions() => new()
    {
        Jobs = new() { ProjectionRebuildBuckets = BucketCount },
        ReadModels = new() { GuardSinkWritesOnWatermark = GuardWrites }
    };

    void Establish()
    {
        _observer.GetSubscription().Returns(new ObserverSubscription("observer-id", _request.ObserverKey, [],
            Collapsing ? typeof(ICollapsingProjectionObserverSubscriber) : typeof(IObserverSubscriber), SiloAddress.Zero));
        _eventStoreStorage.Projections.Has(Arg.Any<ProjectionId>()).Returns(true);
        _eventStoreStorage.Projections.Get(Arg.Any<ProjectionId>()).Returns(new ProjectionDefinition(
            ProjectionOwner.Client, EventSequenceId.Log, "observer-id", ReadModelIdentifier.NotSet, true, true, new(),
            new Dictionary<EventType, FromDefinition> { [new("changed", 1)] = new(new Dictionary<PropertyPath, string>(), new(""), null) },
            new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [],
            new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(), Scope: Independent ? ProjectionScope.Namespaced : ProjectionScope.Global));
    }
}
